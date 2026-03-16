using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Biz.Simulation;
using Iec61850Sim.Core.Model.Devices;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Simulation;

public class SimulationEngineTests
{
    [Fact]
    public void Step_WithDeviceRegisteredBeforeConstruction_ShouldInvokeDeviceStep()
    {
        // Arrange
        var manager = new DeviceManager();
        var device = new TestDevice("D1");
        manager.Register(device);
        var clock = new SimulationClock();
        var engine = new SimulationEngine(clock, manager);

        // Act
        engine.Step(1.25);

        // Assert
        Assert.Equal(1, device.Calls);
        Assert.Equal(1.25, device.LastDt);
    }

    [Fact]
    public void Step_WithDeviceRegisteredAfterConstruction_ShouldNotInvokeDeviceStep()
    {
        // Arrange
        var manager = new DeviceManager();
        var clock = new SimulationClock();
        var engine = new SimulationEngine(clock, manager);
        var lateDevice = new TestDevice("Late");
        manager.Register(lateDevice);

        // Act
        engine.Step(0.1);

        // Assert
        Assert.Equal(0, lateDevice.Calls);
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
