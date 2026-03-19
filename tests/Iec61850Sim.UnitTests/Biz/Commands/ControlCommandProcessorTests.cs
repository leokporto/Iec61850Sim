using IEC61850.Common;
using Iec61850Sim.Core.Biz.Commands;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Model.Devices;
using Iec61850Sim.UnitTests.TestHelpers;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Commands;

public class ControlCommandProcessorTests
{
    private const string BreakerPosRef = "LD0/XCBR1.Pos.stVal";
    private const string CswiPosRef = "LD0/CSWI1.Pos.stVal";
    private const string CswiCmdRef = "LD0/CSWI1.Pos.Oper.ctlVal";

    [Fact]
    public void Operate_WithBooleanTrue_ShouldCloseBreakerAndSyncControllerPosition()
    {
        var (sut, controller, registry) = CreateController();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        sut.Operate(controller, new MmsValue(true), test: false);

        var brkVal = registry.GetValue<object>(BreakerPosRef);
        var cswiVal = registry.GetValue<object>(CswiPosRef);
        Assert.Equal(2, brkVal.Value);
        Assert.Equal(brkVal.Value, cswiVal.Value);
        Assert.True(brkVal.Timestamp >= before);
        Assert.Equal(brkVal.Timestamp, cswiVal.Timestamp);
    }

    [Fact]
    public void Operate_WithBooleanFalse_ShouldOpenBreakerAndSyncControllerPosition()
    {
        var (sut, controller, registry) = CreateController();

        sut.Operate(controller, new MmsValue(false), test: false);

        var brkVal = registry.GetValue<object>(BreakerPosRef);
        var cswiVal = registry.GetValue<object>(CswiPosRef);
        Assert.Equal(1, brkVal.Value);
        Assert.Equal(brkVal.Value, cswiVal.Value);
    }

    [Fact]
    public void Operate_WithIntegerOn_ShouldCloseBreaker()
    {
        var (sut, controller, registry) = CreateController();

        sut.Operate(controller, new MmsValue((int)eDblPos.On), test: false);

        Assert.Equal(2, registry.GetValue<object>(BreakerPosRef).Value);
    }

    [Fact]
    public void Operate_WithIsSelectedFalse_ShouldOpenBreaker()
    {
        var (sut, controller, registry) = CreateController();

        sut.Operate(controller, isSelected: false);

        var brkVal = registry.GetValue<object>(BreakerPosRef);
        var cswiVal = registry.GetValue<object>(CswiPosRef);
        Assert.Equal(1, brkVal.Value);
        Assert.Equal(brkVal.Value, cswiVal.Value);
    }

    [Fact]
    public void Operate_WithIntermediatePosition_ShouldKeepCurrentBreakerValue()
    {
        var (sut, controller, registry) = CreateController();
        // Pre-set breaker to On (2)
        registry.SetValue(new PointValue<object> { Reference = BreakerPosRef, Value = 2, Timestamp = DateTimeOffset.UtcNow, Quality = 192 });

        sut.Operate(controller, new MmsValue((int)eDblPos.Intermediate), test: false);

        Assert.Equal(2, registry.GetValue<object>(BreakerPosRef).Value);
        Assert.Equal(2, registry.GetValue<object>(CswiPosRef).Value);
    }

    private static (ControlCommandProcessor sut, Cswi controller, PointRegistry registry) CreateController()
    {
        var registry = new PointRegistry();

        var breakerPosPoint = DevicePointFactory.CreatePoint(
            BreakerPosRef, "XCBR1", "Pos", eLnType.XCBR, FunctionalConstraint.ST);
        registry.Register(breakerPosPoint);

        var cswiCmdPoint = DevicePointFactory.CreatePoint(
            CswiCmdRef, "CSWI1", "Pos", eLnType.CSWI, FunctionalConstraint.CO,
            withValueAttribute: false);
        registry.Register(cswiCmdPoint);

        var cswiPosPoint = DevicePointFactory.CreatePoint(
            CswiPosRef, "CSWI1", "Pos", eLnType.CSWI, FunctionalConstraint.ST);
        registry.Register(cswiPosPoint);

        var breaker = new Breaker("XCBR1", BreakerPosRef, "Q01", registry);
        var cswi = new Cswi("CSWI1", CswiCmdRef, CswiPosRef, breaker, registry);
        var sut = new ControlCommandProcessor(registry);


        return (sut, cswi, registry);
    }
}
