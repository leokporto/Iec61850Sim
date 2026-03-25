using System.Runtime.CompilerServices;
using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Xunit;

namespace Iec61850Sim.UnitTests.Device;

public class XswiTests
{
    private static class Refs
    {
        public const string Pos    = "LD/XSWI1.Pos.stVal";
        public const string Loc    = "LD/XSWI1.Loc.stVal";
        public const string BlkOpn = "LD/XSWI1.BlkOpn.stVal";
        public const string BlkCls = "LD/XSWI1.BlkCls.stVal";
        public const string SwAbm  = "LD/XSWI1.SwAbm.stVal";
    }

    // ── Blocking: Open ────────────────────────────────────────────────────

    [Fact]
    public void Open_WhenLocIsTrue_ShouldReturnBlockedBySwitchingHierarchy()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.Loc, true);

        var cause = xswi.Open();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY, cause);
    }

    [Fact]
    public void Open_WhenBlkOpnIsTrue_ShouldReturnBlockedByInterlocking()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.BlkOpn, true);

        var cause = xswi.Open();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING, cause);
    }

    [Fact]
    public void Open_WhenBlocked_ShouldNotChangePosition()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.Pos, 2);
        SetValue(registry, Refs.BlkOpn, true);

        xswi.Open();

        Assert.Equal(2, registry.GetValue<object>(Refs.Pos).Value);
    }

    // ── Blocking: Close ───────────────────────────────────────────────────

    [Fact]
    public void Close_WhenLocIsTrue_ShouldReturnBlockedBySwitchingHierarchy()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.Loc, true);

        var cause = xswi.Close();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY, cause);
    }

    [Fact]
    public void Close_WhenBlkClsIsTrue_ShouldReturnBlockedByInterlocking()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.BlkCls, true);

        var cause = xswi.Close();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING, cause);
    }

    [Fact]
    public void Open_WhenBlkClsIsTrue_ShouldOpenNormally()
    {
        // BlkCls only blocks closing
        var (_, xswi) = CreateXswi();

        var cause = xswi.Open();

        Assert.Null(cause);
    }

    [Fact]
    public void Close_WhenBlkOpnIsTrue_ShouldCloseNormally()
    {
        // BlkOpn only blocks opening
        var (_, xswi) = CreateXswi();

        var cause = xswi.Close();

        Assert.Null(cause);
    }

    // ── Maneuver state machine ────────────────────────────────────────────

    [Fact]
    public void Open_WhenNotBlocked_ShouldSetIntermediateImmediately()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.Pos, 2); // currently closed

        xswi.Open();

        Assert.Equal((int)eDblPos.Intermediate, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Close_WhenNotBlocked_ShouldSetIntermediateImmediately()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.Pos, 1); // currently open

        xswi.Close();

        Assert.Equal((int)eDblPos.Intermediate, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Step_BeforeManeuverTimeElapses_ShouldKeepIntermediatePosition()
    {
        var (registry, xswi) = CreateXswi(maneuverTime: 2.0);
        xswi.Open();

        xswi.Step(1.0); // half of maneuver time

        Assert.Equal((int)eDblPos.Intermediate, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Step_WhenManeuverTimeElapses_ShouldSetOpenPosition()
    {
        var (registry, xswi) = CreateXswi(maneuverTime: 2.0);
        SetValue(registry, Refs.Pos, 2);
        xswi.Open();

        xswi.Step(2.0);

        Assert.Equal((int)eDblPos.Off, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Step_WhenManeuverTimeElapses_ShouldSetClosedPosition()
    {
        var (registry, xswi) = CreateXswi(maneuverTime: 2.0);
        SetValue(registry, Refs.Pos, 1);
        xswi.Close();

        xswi.Step(2.0);

        Assert.Equal((int)eDblPos.On, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Step_WhenNoManeuverPending_ShouldNotChangePosition()
    {
        var (registry, xswi) = CreateXswi();
        SetValue(registry, Refs.Pos, 2);

        xswi.Step(5.0);

        Assert.Equal(2, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Step_AccumulatesTimeAcrossMultipleCalls()
    {
        var (registry, xswi) = CreateXswi(maneuverTime: 2.0);
        xswi.Open();

        xswi.Step(1.0);
        xswi.Step(1.0); // total = 2.0 → should complete

        Assert.Equal((int)eDblPos.Off, registry.GetValue<object>(Refs.Pos).Value);
    }

    // ── SwAbm alarm ───────────────────────────────────────────────────────

    [Fact]
    public void Step_WhenTravelExceedsSwAbmTime_ShouldRaiseSwAbm()
    {
        var (registry, xswi) = CreateXswi(maneuverTime: 100.0, swAbmTime: 5.0);
        xswi.Open();

        xswi.Step(6.0); // > swAbmTime but < maneuverTime

        Assert.Equal(true, registry.GetValue<object>(Refs.SwAbm).Value);
    }

    [Fact]
    public void Step_WhenManeuverCompletesAfterSwAbm_ShouldClearSwAbm()
    {
        var (registry, xswi) = CreateXswi(maneuverTime: 10.0, swAbmTime: 5.0);
        xswi.Open();
        xswi.Step(6.0); // raises SwAbm

        xswi.Step(5.0); // total = 11.0 → maneuver complete

        Assert.Equal(false, registry.GetValue<object>(Refs.SwAbm).Value);
    }

    [Fact]
    public void Step_WhenSwAbmRefAbsent_ShouldNotThrow()
    {
        var registry = new PointRegistry();
        RegisterPoint(registry, Refs.Pos, "Pos");
        SetValue(registry, Refs.Pos, 2);

        // xswi without SwAbm ref
        var xswi = new XSWI("XSWI1", Refs.Pos, "Q01", registry, maneuverTime: 1.0, swAbmTime: 0.5);
        xswi.Open();

        var ex = Record.Exception(() => xswi.Step(1.0));

        Assert.Null(ex);
    }

    // ── Initialize ────────────────────────────────────────────────────────

    [Fact]
    public void Initialize_WithTypeDefaultLocValue_ShouldApplyFalseFallback()
    {
        var (registry, xswi) = CreateXswiWithAttributes();

        xswi.Initialize(_ => false);

        Assert.Equal(false, registry.GetValue<object>(Refs.Loc).Value);
    }

    [Fact]
    public void Initialize_WithNonDefaultLocValue_ShouldPreserveCfgValue()
    {
        var (registry, xswi) = CreateXswiWithAttributes();

        xswi.Initialize(_ => true);

        Assert.Equal(true, registry.GetValue<object>(Refs.Loc).Value);
    }

    // ── ISwitchingDevice contract ─────────────────────────────────────────

    [Fact]
    public void Open_WhenNotBlocked_ShouldReturnNull()
    {
        var (_, xswi) = CreateXswi();

        Assert.Null(xswi.Open());
    }

    [Fact]
    public void Close_WhenNotBlocked_ShouldReturnNull()
    {
        var (_, xswi) = CreateXswi();

        Assert.Null(xswi.Close());
    }

    [Fact]
    public void ImplementsISwitchingDevice()
    {
        var (_, xswi) = CreateXswi();
        Assert.IsAssignableFrom<ISwitchingDevice>(xswi);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static (PointRegistry, XSWI) CreateXswi(double maneuverTime = 2.0, double swAbmTime = 30.0)
    {
        var registry = new PointRegistry();
        RegisterPoint(registry, Refs.Pos, "Pos");
        RegisterPoint(registry, Refs.Loc, "Loc");
        RegisterPoint(registry, Refs.BlkOpn, "BlkOpn");
        RegisterPoint(registry, Refs.BlkCls, "BlkCls");
        RegisterPoint(registry, Refs.SwAbm, "SwAbm");

        SetValue(registry, Refs.Pos, 1);
        SetValue(registry, Refs.Loc, false);
        SetValue(registry, Refs.BlkOpn, false);
        SetValue(registry, Refs.BlkCls, false);
        SetValue(registry, Refs.SwAbm, false);

        var xswi = new XSWI("XSWI1", Refs.Pos, "Q01", registry,
            locRef: Refs.Loc,
            blkOpnRef: Refs.BlkOpn,
            blkClsRef: Refs.BlkCls,
            swAbmRef: Refs.SwAbm,
            maneuverTime: maneuverTime,
            swAbmTime: swAbmTime);

        return (registry, xswi);
    }

    private static (PointRegistry, XSWI) CreateXswiWithAttributes()
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var ln = new LogicalNode("XSWI1", ld);

        var registry = new PointRegistry();

        RegisterPoint(registry, Refs.Pos, "Pos");

        var locDo = new DataObject("Loc", ln);
        var locDa = new DataAttribute("stVal", locDo, DataAttributeType.BOOLEAN, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        registry.Register(new DevicePoint
        {
            Reference = Refs.Loc, EquipmentName = "Q01", LogicalDevice = "LD",
            LogicalNode = "XSWI1", DataObject = "Loc", LnType = eLnType.XSWI,
            Fc = FunctionalConstraint.ST, ValueAttribute = locDa
        });

        var xswi = new XSWI("XSWI1", Refs.Pos, "Q01", registry, locRef: Refs.Loc);
        return (registry, xswi);
    }

    private static void RegisterPoint(PointRegistry registry, string reference, string dataObject)
        => registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "Q01",
            LogicalDevice = "LD",
            LogicalNode = "XSWI1",
            DataObject = dataObject,
            LnType = eLnType.XSWI,
            Fc = FunctionalConstraint.ST,
            ValueAttribute = (DataAttribute)RuntimeHelpers.GetUninitializedObject(typeof(DataAttribute))
        });

    private static void SetValue(PointRegistry registry, string reference, object value)
        => registry.SetValue(new PointValue<object>
        {
            Reference = reference,
            Value = value,
            Quality = 192,
            Timestamp = DateTimeOffset.UtcNow
        });
}
