using System.Runtime.CompilerServices;
using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Xunit;

namespace Iec61850Sim.UnitTests.Device;

public class CswiTests
{
    private const string BreakerPosRef = "LD/XCBR1.Pos.stVal";
    private const string BreakerLocRef = "LD/XCBR1.Loc.stVal";
    private const string CswiPosRef = "LD/CSWI1.Pos.stVal";
    private const string CswiCmdRef = "LD/CSWI1.Pos.Oper.ctlVal";
    private const string CswiLocRef = "LD/CSWI1.Loc.stVal";
    private const string CswiOpCntRsRef = "LD/CSWI1.OpCntRs.stVal";

    [Fact]
    public void SyncLoc_ShouldUpdateCswiLocInRegistry()
    {
        var (registry, cswi) = CreateCswiWithLoc();

        cswi.SyncLoc(true);

        Assert.Equal(true, registry.GetValue<object>(CswiLocRef).Value);
    }

    [Fact]
    public void SyncLoc_WhenXcbrHasLocRef_ShouldAlsoUpdateXcbrLoc()
    {
        var (registry, cswi) = CreateCswiWithLoc(withXcbrLoc: true);

        cswi.SyncLoc(true);

        Assert.Equal(true, registry.GetValue<object>(CswiLocRef).Value);
        Assert.Equal(true, registry.GetValue<object>(BreakerLocRef).Value);
    }

    [Fact]
    public void SyncLoc_WhenXcbrHasNoLocRef_ShouldOnlyUpdateCswiLoc()
    {
        var (registry, cswi) = CreateCswiWithLoc(withXcbrLoc: false);

        cswi.SyncLoc(true);

        Assert.Equal(true, registry.GetValue<object>(CswiLocRef).Value);
        Assert.False(registry.All.Any(p => p.Reference == BreakerLocRef));
    }

    [Fact]
    public void SyncLoc_ToFalse_ShouldSetBothToFalse()
    {
        var (registry, cswi) = CreateCswiWithLoc(withXcbrLoc: true);
        SetValue(registry, CswiLocRef, true);
        SetValue(registry, BreakerLocRef, true);

        cswi.SyncLoc(false);

        Assert.Equal(false, registry.GetValue<object>(CswiLocRef).Value);
        Assert.Equal(false, registry.GetValue<object>(BreakerLocRef).Value);
    }

    [Fact]
    public void Initialize_WithDefaultLocValue_ShouldSetFalse()
    {
        var (registry, cswi) = CreateCswiWithAttributes();

        cswi.Initialize(_ => false);

        Assert.Equal(false, registry.GetValue<object>(CswiLocRef).Value);
    }

    [Fact]
    public void Initialize_WithTrueLocInCfg_ShouldPreserveCfgValue()
    {
        var (registry, cswi) = CreateCswiWithAttributes();

        cswi.Initialize(_ => true);

        Assert.Equal(true, registry.GetValue<object>(CswiLocRef).Value);
    }

    [Fact]
    public void Initialize_WithDefaultOpCntRs_ShouldSetZero()
    {
        var (registry, cswi) = CreateCswiWithAttributes();

        cswi.Initialize(_ => 0);

        Assert.Equal(0, registry.GetValue<object>(CswiOpCntRsRef).Value);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static (PointRegistry, CSWI) CreateCswiWithLoc(bool withXcbrLoc = true)
    {
        var registry = new PointRegistry();

        RegisterPoint(registry, BreakerPosRef, "Pos", eLnType.XCBR, FunctionalConstraint.ST);
        RegisterPoint(registry, CswiCmdRef, "Pos", eLnType.CSWI, FunctionalConstraint.CO);
        RegisterPoint(registry, CswiPosRef, "Pos", eLnType.CSWI, FunctionalConstraint.ST);
        RegisterPoint(registry, CswiLocRef, "Loc", eLnType.CSWI, FunctionalConstraint.ST);

        SetValue(registry, BreakerPosRef, 1);
        SetValue(registry, CswiLocRef, false);

        string? xcbrLocRef = null;
        if (withXcbrLoc)
        {
            RegisterPoint(registry, BreakerLocRef, "Loc", eLnType.XCBR, FunctionalConstraint.ST);
            SetValue(registry, BreakerLocRef, false);
            xcbrLocRef = BreakerLocRef;
        }

        var xcbr = new XCBR("XCBR1", BreakerPosRef, "Q01", registry, locRef: xcbrLocRef);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, xcbr, registry, locRef: CswiLocRef);

        return (registry, cswi);
    }

    private static (PointRegistry, CSWI) CreateCswiWithAttributes()
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var cswiLn = new LogicalNode("CSWI1", ld);
        var xcbrLn = new LogicalNode("XCBR1", ld);

        var registry = new PointRegistry();

        RegisterPoint(registry, BreakerPosRef, "Pos", eLnType.XCBR, FunctionalConstraint.ST);
        RegisterPoint(registry, CswiCmdRef, "Pos", eLnType.CSWI, FunctionalConstraint.CO);
        RegisterPoint(registry, CswiPosRef, "Pos", eLnType.CSWI, FunctionalConstraint.ST);

        var locDo = new DataObject("Loc", cswiLn);
        var locDa = new DataAttribute("stVal", locDo, DataAttributeType.BOOLEAN, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        registry.Register(new DevicePoint
        {
            Reference = CswiLocRef, EquipmentName = "", LogicalDevice = "LD",
            LogicalNode = "CSWI1", DataObject = "Loc", LnType = eLnType.CSWI,
            Fc = FunctionalConstraint.ST, ValueAttribute = locDa
        });

        var opCntRsDo = new DataObject("OpCntRs", cswiLn);
        var opCntRsDa = new DataAttribute("stVal", opCntRsDo, DataAttributeType.INT32, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        registry.Register(new DevicePoint
        {
            Reference = CswiOpCntRsRef, EquipmentName = "", LogicalDevice = "LD",
            LogicalNode = "CSWI1", DataObject = "OpCntRs", LnType = eLnType.CSWI,
            Fc = FunctionalConstraint.ST, ValueAttribute = opCntRsDa
        });

        var xcbr = new XCBR("XCBR1", BreakerPosRef, "Q01", registry);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, xcbr, registry,
            locRef: CswiLocRef, opCntRsRef: CswiOpCntRsRef);

        return (registry, cswi);
    }

    private static void RegisterPoint(PointRegistry registry, string reference, string dataObject, eLnType lnType, FunctionalConstraint fc)
        => registry.Register(new DevicePoint
        {
            Reference = reference, EquipmentName = "", LogicalDevice = "LD",
            LogicalNode = lnType == eLnType.XCBR ? "XCBR1" : "CSWI1",
            DataObject = dataObject, LnType = lnType, Fc = fc,
            ValueAttribute = (DataAttribute)RuntimeHelpers.GetUninitializedObject(typeof(DataAttribute))
        });

    private static void SetValue(PointRegistry registry, string reference, object value)
        => registry.SetValue(new PointValue<object> { Reference = reference, Value = value, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
}