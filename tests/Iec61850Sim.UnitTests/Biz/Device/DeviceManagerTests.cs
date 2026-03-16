using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Devices;
using Iec61850Sim.UnitTests.TestHelpers;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Device;

public class DeviceManagerTests
{
    [Fact]
    public void Register_WithBreaker_ShouldAddToDevicesAndBreakers()
    {
        // Arrange
        var manager = new DeviceManager();
        var breaker = new Breaker(
            name: "XCBR1",
            pos: DevicePointFactory.CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", "Pos", eLnType.XCBR, IEC61850.Common.FunctionalConstraint.ST));

        // Act
        manager.Register(breaker);

        // Assert
        Assert.Single(manager.Devices);
        Assert.Single(manager.Breakers);
        Assert.Same(breaker, manager.Breakers[0]);
    }

    [Fact]
    public void Register_WithMeasurementDevice_ShouldAddToDevicesAndMeasurements()
    {
        // Arrange
        var manager = new DeviceManager();
        var point = DevicePointFactory.CreatePoint(
            "LD0/MMXU1.PhV.phsA.cVal.mag.f",
            "MMXU1",
            "PhV",
            eLnType.MMXU,
            IEC61850.Common.FunctionalConstraint.MX);
        var measurement = new MeasurementDevice("MMXU1", [point]);

        // Act
        manager.Register(measurement);

        // Assert
        Assert.Single(manager.Devices);
        Assert.Single(manager.Measurements);
        Assert.Same(measurement, manager.Measurements[0]);
    }

    [Fact]
    public void RegisterController_WithCswi_ShouldAddToControllersOnly()
    {
        // Arrange
        var manager = new DeviceManager();
        var breaker = new Breaker(
            "XCBR1",
            DevicePointFactory.CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", "Pos", eLnType.XCBR, IEC61850.Common.FunctionalConstraint.ST));
        var cmd = DevicePointFactory.CreatePoint("LD0/CSWI1.Pos.Oper.ctlVal", "CSWI1", "Pos", eLnType.CSWI, IEC61850.Common.FunctionalConstraint.CO, withValueAttribute: false);
        var pos = DevicePointFactory.CreatePoint("LD0/CSWI1.Pos.stVal", "CSWI1", "Pos", eLnType.CSWI, IEC61850.Common.FunctionalConstraint.ST);
        var cswi = new Cswi("CSWI1", cmd, pos, breaker);

        // Act
        manager.RegisterController(cswi);

        // Assert
        Assert.Single(manager.Controllers);
        Assert.Empty(manager.Devices);
    }

    [Fact]
    public void Step_WithRegisteredDevices_ShouldInvokeStepOnAllDevices()
    {
        // Arrange
        var manager = new DeviceManager();
        var first = new TestDevice("A");
        var second = new TestDevice("B");

        manager.Register(first);
        manager.Register(second);

        // Act
        manager.Step(0.25);

        // Assert
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
        Assert.Equal(0.25, first.LastDt);
        Assert.Equal(0.25, second.LastDt);
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
