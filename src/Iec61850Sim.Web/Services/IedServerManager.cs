using Iec61850Sim.Core;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Model.Tree;
using Iec61850Sim.Core.Points;
using Iec61850Sim.Core.Scl;
using Iec61850Sim.Core.Simulation;

using IEC61850.Server;

namespace Iec61850Sim.Web.Services;

/// <summary>
/// Orchestrates safe stop and restart of the IEC 61850 server.
///
/// Registered as singleton (not scoped) because the IEC server is application-level state
/// shared across all Blazor Server circuits. A scoped instance would create per-circuit
/// instances that would race to stop/restart the same underlying server.
/// </summary>
public sealed class IedServerManager
{
    private readonly DeviceManager _deviceManager;
    private readonly IecServerHost _iecHost;
    private readonly IedModel _model;
    private readonly PointRegistry _registry;
    private readonly RuntimeEngine _runtime;
    private readonly ISclConversionService _sclConverter;
    private readonly SimulationEngine _simulationEngine;
    private volatile bool _isRestarting;

    public IedServerManager(
        RuntimeEngine runtime,
        IecServerHost iecHost,
        PointRegistry registry,
        DeviceManager deviceManager,
        SimulationEngine simulationEngine,
        ISclConversionService sclConverter,
        IedModel model)
    {
        _runtime = runtime;
        _iecHost = iecHost;
        _registry = registry;
        _deviceManager = deviceManager;
        _simulationEngine = simulationEngine;
        _sclConverter = sclConverter;
        _model = model;
    }

    public bool IsRestarting => _isRestarting;

    // ─────────────────────────────────────────────────────────────────────────
    // DI factory methods — called by the container to produce singletons.
    // Centralises model-loading logic previously scattered across Program.cs.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DI factory for <see cref="IedModel"/>. Resolves the model path from the
    /// environment, converts SCL to CFG when necessary, and parses the result.
    /// Throws on failure so the application fails fast at startup.
    /// </summary>
    public static IedModel CreateModel(IServiceProvider sp)
    {
        var modelPath = Environment.GetEnvironmentVariable("IEC_MODEL") ?? "./Config/Demo_Ed2.icd";
        var sclConverter = sp.GetRequiredService<ISclConversionService>();

        var absolutePath = Path.IsPathRooted(modelPath)
            ? modelPath
            : Path.Combine(AppContext.BaseDirectory, modelPath);

        if (SclConversionService.IsSclFile(absolutePath))
        {
            Console.WriteLine($"[Startup] SCL file detected, converting: {absolutePath}");
            absolutePath = sclConverter.ConvertToCfg(absolutePath);
        }
        else if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"CFG file not found: {absolutePath}", absolutePath);
        }

        var model = ConfigFileParser.CreateModelFromConfigFile(absolutePath)
                    ?? throw new InvalidOperationException($"No valid data model found in: {absolutePath}");

        model.SetIedName(sclConverter.GetIedName());
        return model;
    }

    /// <summary>
    /// DI factory for <see cref="IModelNode"/>. Builds the UI model tree from the
    /// already-resolved <see cref="IedModel"/> and <see cref="ISclConversionService"/>.
    /// </summary>
    public static IModelNode CreateModelNode(IServiceProvider sp)
    {
        var model = sp.GetRequiredService<IedModel>();
        var registry = sp.GetRequiredService<PointRegistry>();
        var sclConverter = sp.GetRequiredService<ISclConversionService>();
        return new ModelTreeBuilder().Build(sclConverter.GetIedName(), model, registry);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Startup initialization — called once from Program.cs after app.Build().
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans the startup model and builds the device registry.
    /// Must be called once after the DI container is built.
    /// </summary>
    /// <remarks>
    /// <see cref="SimulationEngine"/> and <see cref="IecServerHost"/> are constructed by DI
    /// before this method runs (as transitive dependencies of this class), so their
    /// constructors see an empty registry and device manager. This method calls
    /// <see cref="SimulationEngine.ReRegisterDevices"/> and
    /// <see cref="IecServerHost.RefreshRegistrations"/> to fix them up after scan+build.
    /// </remarks>
    public void Initialize()
    {
        ScanAndBuild(_model, _sclConverter.GetLogicalDeviceNames());
        _simulationEngine.ReRegisterDevices();
        _iecHost.RefreshRegistrations(_model);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hot-reload (Settings page) — stops, reloads, restarts.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stops the current simulation and server, clears all state, reloads the model from
    /// <paramref name="modelPath"/>, and restarts on <paramref name="port"/>.
    /// </summary>
    public async Task RestartAsync(string modelPath, string serverModel, int port)
    {
        if (_isRestarting)
            return;

        _isRestarting = true;
        try
        {
            // 1. Stop simulation and server cleanly
            _runtime.StopSimulation();
            _runtime.StopServer();

            // 2. Clear all accumulated state
            _registry.Clear();
            _deviceManager.Clear();

            // 3. Allow time for clean teardown
            await Task.Delay(2000);

            // 4. Load model from new path (SCL or CFG)
            var (newModel, iedName, ldNames) = LoadModel(modelPath, serverModel);
            if (newModel == null)
            {
                Console.WriteLine($"[IedServerManager] Failed to load model from: {modelPath}");
                return;
            }

            // 5. Re-scan model and rebuild devices
            ScanAndBuild(newModel, ldNames);
            _simulationEngine.ReRegisterDevices();

            // 6. Reinitialize IEC server with new model (registers control handlers, no Start yet)
            _iecHost.Reinitialize(newModel, iedName);

            // 7. Start server on new port and initialize device state from model defaults
            _runtime.StartServer(port);
        }
        finally
        {
            _isRestarting = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the model path, converts SCL to CFG when needed, and parses the CFG.
    /// Returns a null model when loading fails — caller is responsible for logging and aborting.
    /// </summary>
    private (IedModel? model, string iedName, IList<string> ldNames) LoadModel(string modelPath, string fallbackIedName)
    {
        var absolutePath = Path.IsPathRooted(modelPath)
            ? modelPath
            : Path.Combine(AppContext.BaseDirectory, modelPath);

        string iedName = fallbackIedName;
        IList<string> ldNames = [];

        if (SclConversionService.IsSclFile(absolutePath))
        {
            absolutePath = _sclConverter.ConvertToCfg(absolutePath);
            iedName = _sclConverter.GetIedName();
            ldNames = _sclConverter.GetLogicalDeviceNames();
        }

        var model = ConfigFileParser.CreateModelFromConfigFile(absolutePath);
        if (model == null)
            return (null, iedName, ldNames);

        model.SetIedName(iedName);
        return (model, iedName, ldNames);
    }

    /// <summary>
    /// Scans <paramref name="model"/> into the registry and rebuilds the device tree.
    /// Shared by <see cref="Initialize"/> (startup) and <see cref="RestartAsync"/> (hot reload).
    /// </summary>
    private void ScanAndBuild(IedModel model, IList<string> ldNames)
    {
        new ModelScanner().Scan(model, _registry, ldNames);
        new DeviceBuilder().Build(_registry, _deviceManager);
        Console.WriteLine($"Devices loaded: {_deviceManager.Devices.Count}");
    }
}
