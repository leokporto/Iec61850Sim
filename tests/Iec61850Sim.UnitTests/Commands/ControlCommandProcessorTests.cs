using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Commands;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Iec61850Sim.UnitTests.TestHelpers;
using Xunit;

namespace Iec61850Sim.UnitTests.Commands;

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

        var result = sut.Operate(controller, new MmsValue(true), test: false);

        Assert.True(result.Success);
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

    // ── CSWI Loc blocking ─────────────────────────────────────────────────

    [Fact]
    public void Operate_WhenCswiLocIsTrue_ShouldNotChangePosition()
    {
        var (sut, controller, registry) = CreateControllerWithLoc();
        registry.SetValue(new PointValue<object> { Reference = CswiLocRef, Value = true, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
        registry.SetValue(new PointValue<object> { Reference = BreakerPosRef, Value = 1, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var result = sut.Operate(controller, new MmsValue(true), test: false);

        Assert.False(result.Success);
        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY, result.Cause);
        Assert.Equal(1, registry.GetValue<object>(BreakerPosRef).Value);
    }

    [Fact]
    public void Operate_WhenCswiLocIsFalse_ShouldOperateNormally()
    {
        var (sut, controller, registry) = CreateControllerWithLoc();
        registry.SetValue(new PointValue<object> { Reference = CswiLocRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        sut.Operate(controller, new MmsValue(true), test: false);

        Assert.Equal(2, registry.GetValue<object>(BreakerPosRef).Value);
    }

    // ── OpCntRs ───────────────────────────────────────────────────────────

    [Fact]
    public void Operate_WhenSuccessful_ShouldIncrementOpCntRs()
    {
        var (sut, controller, registry) = CreateControllerWithOptionals();
        registry.SetValue(new PointValue<object> { Reference = CswiOpCntRsRef, Value = 2, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        sut.Operate(controller, new MmsValue(true), test: false);

        Assert.Equal(3, registry.GetValue<object>(CswiOpCntRsRef).Value);
    }

    [Fact]
    public void Operate_WhenSuccessfulOpen_ShouldSetOpOpnTrueAndOpClsFalse()
    {
        var (sut, controller, registry) = CreateControllerWithOptionals();

        sut.Operate(controller, new MmsValue(false), test: false); // open

        Assert.Equal(true, registry.GetValue<object>(CswiOpOpnRef).Value);
        Assert.Equal(false, registry.GetValue<object>(CswiOpClsRef).Value);
    }

    [Fact]
    public void Operate_WhenSuccessfulClose_ShouldSetOpClsTrueAndOpOpnFalse()
    {
        var (sut, controller, registry) = CreateControllerWithOptionals();

        sut.Operate(controller, new MmsValue(true), test: false); // close

        Assert.Equal(false, registry.GetValue<object>(CswiOpOpnRef).Value);
        Assert.Equal(true, registry.GetValue<object>(CswiOpClsRef).Value);
    }

    [Fact]
    public void Operate_WhenIntermediatePosition_ShouldNotIncrementOpCntRs()
    {
        var (sut, controller, registry) = CreateControllerWithOptionals();
        registry.SetValue(new PointValue<object> { Reference = CswiOpCntRsRef, Value = 5, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        sut.Operate(controller, new MmsValue((int)eDblPos.Intermediate), test: false);

        Assert.Equal(5, registry.GetValue<object>(CswiOpCntRsRef).Value);
    }

    // ── Position-reached ──────────────────────────────────────────────────

    [Fact]
    public void Operate_WhenBreakerAlreadyOpen_ShouldReturnPositionReached()
    {
        var (sut, controller, registry) = CreateController();
        registry.SetValue(new PointValue<object> { Reference = BreakerPosRef, Value = 1, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var result = sut.Operate(controller, new MmsValue(false), test: false); // Open command

        Assert.False(result.Success);
        Assert.Equal(ControlAddCause.ADD_CAUSE_POSITION_REACHED, result.Cause);
        Assert.Equal(1, registry.GetValue<object>(BreakerPosRef).Value);
    }

    [Fact]
    public void Operate_WhenBreakerAlreadyClosed_ShouldReturnPositionReached()
    {
        var (sut, controller, registry) = CreateController();
        registry.SetValue(new PointValue<object> { Reference = BreakerPosRef, Value = 2, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var result = sut.Operate(controller, new MmsValue(true), test: false); // Close command

        Assert.False(result.Success);
        Assert.Equal(ControlAddCause.ADD_CAUSE_POSITION_REACHED, result.Cause);
        Assert.Equal(2, registry.GetValue<object>(BreakerPosRef).Value);
    }

    // ── XCBR blocking ─────────────────────────────────────────────────────

    [Fact]
    public void Operate_WhenXcbrBlkOpnIsTrue_ShouldReturnBlockedByInterlocking()
    {
        var (sut, controller, registry) = CreateControllerWithXcbrBlocking();
        registry.SetValue(new PointValue<object> { Reference = XcbrBlkOpnRef, Value = true, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var result = sut.Operate(controller, new MmsValue(false), test: false); // Open command

        Assert.False(result.Success);
        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING, result.Cause);
    }

    [Fact]
    public void Operate_WhenXcbrBlkClsIsTrue_ShouldReturnBlockedByInterlocking()
    {
        var (sut, controller, registry) = CreateControllerWithXcbrBlocking();
        registry.SetValue(new PointValue<object> { Reference = XcbrBlkClsRef, Value = true, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var result = sut.Operate(controller, new MmsValue(true), test: false); // Close command

        Assert.False(result.Success);
        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING, result.Cause);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private const string XcbrBlkOpnRef = "LD0/XCBR1.BlkOpn.stVal";
    private const string XcbrBlkClsRef = "LD0/XCBR1.BlkCls.stVal";
    private const string CswiLocRef = "LD0/CSWI1.Loc.stVal";
    private const string CswiOpCntRsRef = "LD0/CSWI1.OpCntRs.stVal";
    private const string CswiOpOpnRef = "LD0/CSWI1.OpOpn.stVal";
    private const string CswiOpClsRef = "LD0/CSWI1.OpCls.stVal";

    private static (ControlCommandProcessor sut, CSWI controller, PointRegistry registry) CreateController()
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

        var breaker = new XCBR("XCBR1", BreakerPosRef, "Q01", registry);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, breaker, registry);
        var sut = new ControlCommandProcessor(registry);

        return (sut, cswi, registry);
    }

    private static (ControlCommandProcessor sut, CSWI controller, PointRegistry registry) CreateControllerWithLoc()
    {
        var (sut, cswi, registry) = CreateController();

        registry.Register(DevicePointFactory.CreatePoint(CswiLocRef, "CSWI1", "Loc", eLnType.CSWI, FunctionalConstraint.ST));
        registry.SetValue(new PointValue<object> { Reference = CswiLocRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var cswiWithLoc = new CSWI(cswi.Name, cswi.CommandReference, cswi.PositionReference, cswi.SwitchDevice, registry, locRef: CswiLocRef);

        return (sut, cswiWithLoc, registry);
    }

    private static (ControlCommandProcessor sut, CSWI controller, PointRegistry registry) CreateControllerWithXcbrBlocking()
    {
        var (sut, _, registry) = CreateController();

        registry.Register(DevicePointFactory.CreatePoint(XcbrBlkOpnRef, "XCBR1", "BlkOpn", eLnType.XCBR, FunctionalConstraint.ST));
        registry.Register(DevicePointFactory.CreatePoint(XcbrBlkClsRef, "XCBR1", "BlkCls", eLnType.XCBR, FunctionalConstraint.ST));

        registry.SetValue(new PointValue<object> { Reference = XcbrBlkOpnRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
        registry.SetValue(new PointValue<object> { Reference = XcbrBlkClsRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var breaker = new XCBR("XCBR1", BreakerPosRef, "Q01", registry, blkOpnRef: XcbrBlkOpnRef, blkClsRef: XcbrBlkClsRef);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, breaker, registry);

        return (sut, cswi, registry);
    }

    private static (ControlCommandProcessor sut, CSWI controller, PointRegistry registry) CreateControllerWithOptionals()
    {
        var (sut, _, registry) = CreateController();

        registry.Register(DevicePointFactory.CreatePoint(CswiOpCntRsRef, "CSWI1", "OpCntRs", eLnType.CSWI, FunctionalConstraint.ST));
        registry.Register(DevicePointFactory.CreatePoint(CswiOpOpnRef, "CSWI1", "OpOpn", eLnType.CSWI, FunctionalConstraint.ST));
        registry.Register(DevicePointFactory.CreatePoint(CswiOpClsRef, "CSWI1", "OpCls", eLnType.CSWI, FunctionalConstraint.ST));

        registry.SetValue(new PointValue<object> { Reference = CswiOpCntRsRef, Value = 0, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
        registry.SetValue(new PointValue<object> { Reference = CswiOpOpnRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
        registry.SetValue(new PointValue<object> { Reference = CswiOpClsRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var breaker = new XCBR("XCBR1", BreakerPosRef, "Q01", registry);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, breaker, registry,
            opCntRsRef: CswiOpCntRsRef, opOpnRef: CswiOpOpnRef, opClsRef: CswiOpClsRef);

        return (sut, cswi, registry);
    }
}