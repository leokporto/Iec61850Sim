using System.Reflection;
using IEC61850.Server;
using Iec61850Sim.Core.Biz;
using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Biz.Simulation;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Devices;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz;

public class RuntimeEngineTests
{
    [Fact]
    public void StartSimulation_WhenServerAlreadyRunning_ShouldSetSimulationRunningTrue()
    {
        // Arrange
        var engine = CreateRuntimeEngine();
        SetPrivateAutoProperty(engine, "ServerRunning", true);

        // Act
        engine.StartSimulation();

        // Assert
        Assert.True(engine.SimulationRunning);
        Assert.True(engine.ServerRunning);
    }

    [Fact]
    public void StopSimulation_WhenCalled_ShouldSetSimulationRunningFalse()
    {
        // Arrange
        var engine = CreateRuntimeEngine();
        SetPrivateAutoProperty(engine, "SimulationRunning", true);

        // Act
        engine.StopSimulation();

        // Assert
        Assert.False(engine.SimulationRunning);
    }

    [Fact]
    public void Step_WhenCalled_ShouldAdvanceSimulationEngine()
    {
        // Arrange
        var manager = new DeviceManager();
        var device = new TestDevice("D1");
        manager.Register(device);
        var clock = new SimulationClock();
        var simulation = new SimulationEngine(clock, manager);
        IecServerHost host = null!;
        IedModel model = null!;
        var engine = new RuntimeEngine(model, manager, simulation, host);

        // Act
        engine.Step(0.33);

        // Assert
        Assert.Equal(1, device.Calls);
        Assert.Equal(0.33, device.LastDt);
    }

    [Fact]
    public void StartServer_WhenAlreadyRunning_ShouldKeepServerRunningTrue()
    {
        // Arrange
        var engine = CreateRuntimeEngine();
        SetPrivateAutoProperty(engine, "ServerRunning", true);

        // Act
        engine.StartServer();

        // Assert
        Assert.True(engine.ServerRunning);
    }

    [Fact]
    public void StopServer_WhenAlreadyStopped_ShouldKeepServerRunningFalse()
    {
        // Arrange
        var engine = CreateRuntimeEngine();
        SetPrivateAutoProperty(engine, "ServerRunning", false);

        // Act
        engine.StopServer();

        // Assert
        Assert.False(engine.ServerRunning);
    }

    private static RuntimeEngine CreateRuntimeEngine()
    {
        var manager = new DeviceManager();
        var simulation = new SimulationEngine(new SimulationClock(), manager);
        IecServerHost host = null!;
        IedModel model = null!;
        return new RuntimeEngine(model, manager, simulation, host);
    }

    private static void SetPrivateAutoProperty<T>(RuntimeEngine engine, string propertyName, T value)
    {
        var field = typeof(RuntimeEngine).GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(engine, value);
    }

    private sealed class TestDevice(string name) : DeviceBase(name)
    {
        public int Calls { get; private set; }
        public double LastDt { get; private set; }

        public override void Step(double dt)
        {
            Calls++;
            LastDt = dt;
        }
    }
}
