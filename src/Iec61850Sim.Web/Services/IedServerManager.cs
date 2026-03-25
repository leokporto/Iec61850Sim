using Iec61850Sim.Core;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
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
    private readonly RuntimeEngine _runtime;
    private readonly IecServerHost _iecHost;
    private readonly PointRegistry _registry;
    private readonly DeviceManager _deviceManager;
    private readonly SimulationEngine _simulationEngine;
    private volatile bool _isRestarting;

    public bool IsRestarting => _isRestarting;

    public IedServerManager(
        RuntimeEngine runtime,
        IecServerHost iecHost,
        PointRegistry registry,
        DeviceManager deviceManager,
        SimulationEngine simulationEngine)
    {
        _runtime = runtime;
        _iecHost = iecHost;
        _registry = registry;
        _deviceManager = deviceManager;
        _simulationEngine = simulationEngine;
    }

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

            // 4. Load model from new path
            var cfgPath = Path.Combine(AppContext.BaseDirectory, modelPath);
            var newModel = ConfigFileParser.CreateModelFromConfigFile(cfgPath);
            if (newModel == null)
            {
                Console.WriteLine($"[IedServerManager] Failed to load model from: {cfgPath}");
                return;
            }
            newModel.SetIedName("Demo");

            // 5. Re-scan model and rebuild devices
            new ModelScanner().Scan(newModel, _registry, ["Measurement", "ProtCtrl"]);
            new DeviceBuilder().Build(_registry, _deviceManager);
            _simulationEngine.ReRegisterDevices();

            // 6. Reinitialize IEC server with new model (registers control handlers, no Start yet)
            _iecHost.Reinitialize(newModel, serverModel);

            // 7. Start server on new port and initialize device state from model defaults
            _runtime.StartServer(port);
        }
        finally
        {
            _isRestarting = false;
        }
    }
}
