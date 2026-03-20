using IEC61850.Common;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Iec61850Sim.UnitTests.TestHelpers;
using Xunit;

namespace Iec61850Sim.UnitTests.Device;

public class DeviceManagerTests
{
    [Fact]
    public void Register_WithBreaker_ShouldAddToDevicesAndBreakers()
    {
        var registry = new PointRegistry();
        registry.Register(DevicePointFactory.CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", "Pos", eLnType.XCBR, FunctionalConstraint.ST));

        var manager = new DeviceManager();
        var breaker = new XCBR("XCBR1", "LD0/XCBR1.Pos.stVal", "Q01", registry);

        manager.Register(breaker);

        Assert.Single(manager.Devices);
        Assert.Single(manager.Breakers);
        Assert.Same(breaker, manager.Breakers[0]);
    }

    [Fact]
    public void Register_WithMeasurementDevice_ShouldAddToDevicesAndMeasurements()
    {
        var registry = new PointRegistry();
        registry.Register(DevicePointFactory.CreatePoint("LD0/MMXU1.PhV.phsA.cVal.mag.f", "MMXU1", "PhV", eLnType.MMXU, FunctionalConstraint.MX));

        var manager = new DeviceManager();
        var measurement = new MeasurementDevice("MMXU1", ["LD0/MMXU1.PhV.phsA.cVal.mag.f"], registry);

        manager.Register(measurement);

        Assert.Single(manager.Devices);
        Assert.Single(manager.Measurements);
        Assert.Same(measurement, manager.Measurements[0]);
    }

    [Fact]
    public void RegisterController_WithCswi_ShouldAddToControllersOnly()
    {
        var registry = new PointRegistry();
        registry.Register(DevicePointFactory.CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", "Pos", eLnType.XCBR, FunctionalConstraint.ST));
        registry.Register(DevicePointFactory.CreatePoint("LD0/CSWI1.Pos.Oper.ctlVal", "CSWI1", "Pos", eLnType.CSWI, FunctionalConstraint.CO, withValueAttribute: false));
        registry.Register(DevicePointFactory.CreatePoint("LD0/CSWI1.Pos.stVal", "CSWI1", "Pos", eLnType.CSWI, FunctionalConstraint.ST));

        var manager = new DeviceManager();
        var breaker = new XCBR("XCBR1", "LD0/XCBR1.Pos.stVal", "Q01", registry);
        var cswi = new CSWI("CSWI1", "LD0/CSWI1.Pos.Oper.ctlVal", "LD0/CSWI1.Pos.stVal", breaker, registry);

        manager.RegisterController(cswi);

        Assert.Single(manager.Controllers);
        Assert.Empty(manager.Devices);
    }

    [Fact]
    public void Step_WithRegisteredDevices_ShouldInvokeStepOnAllDevices()
    {
        var manager = new DeviceManager();
        var first = new TestDevice("A");
        var second = new TestDevice("B");

        manager.Register(first);
        manager.Register(second);

        manager.Step(0.25);

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