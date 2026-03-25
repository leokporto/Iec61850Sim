using System.Runtime.CompilerServices;
using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Xunit;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Iec61850Sim.UnitTests.Device;

public class XcbrTests
{
    // ── Blocking behaviour ────────────────────────────────────────────────

    [Fact]
    public void Open_WhenLocIsTrue_ShouldNotChangePosition()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Pos, 2);  // currently closed
        SetValue(registry, Refs.Loc, true);

        xcbr.Open();

        Assert.Equal(2, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Open_WhenBlkOpnIsTrue_ShouldNotChangePosition()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Pos, 2);
        SetValue(registry, Refs.BlkOpn, true);

        xcbr.Open();

        Assert.Equal(2, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Close_WhenLocIsTrue_ShouldNotChangePosition()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Pos, 1);  // currently open
        SetValue(registry, Refs.Loc, true);

        xcbr.Close();

        Assert.Equal(1, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Close_WhenBlkClsIsTrue_ShouldNotChangePosition()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Pos, 1);
        SetValue(registry, Refs.BlkCls, true);

        xcbr.Close();

        Assert.Equal(1, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Open_WhenBlkClsIsTrue_ShouldOpenNormally()
    {
        // BlkCls only blocks closing, not opening
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Pos, 2);
        SetValue(registry, Refs.BlkCls, true);

        xcbr.Open();

        Assert.Equal(1, registry.GetValue<object>(Refs.Pos).Value);
    }

    [Fact]
    public void Close_WhenBlkOpnIsTrue_ShouldCloseNormally()
    {
        // BlkOpn only blocks opening, not closing
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Pos, 1);
        SetValue(registry, Refs.BlkOpn, true);

        xcbr.Close();

        Assert.Equal(2, registry.GetValue<object>(Refs.Pos).Value);
    }

    // ── OpCnt ─────────────────────────────────────────────────────────────

    [Fact]
    public void Open_WhenNotBlocked_ShouldIncrementOpCnt()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.OpCnt, 3);

        xcbr.Open();

        Assert.Equal(4, registry.GetValue<object>(Refs.OpCnt).Value);
    }

    [Fact]
    public void Close_WhenNotBlocked_ShouldIncrementOpCnt()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.OpCnt, 0);

        xcbr.Close();

        Assert.Equal(1, registry.GetValue<object>(Refs.OpCnt).Value);
    }

    [Fact]
    public void Open_WhenBlocked_ShouldNotIncrementOpCnt()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Pos, 2);
        SetValue(registry, Refs.Loc, true);
        SetValue(registry, Refs.OpCnt, 5);

        xcbr.Open();

        Assert.Equal(5, registry.GetValue<object>(Refs.OpCnt).Value);
    }

    // ── Return values ─────────────────────────────────────────────────────

    [Fact]
    public void Open_WhenNotBlocked_ShouldReturnNull()
    {
        var (registry, xcbr) = CreateXcbr();

        var cause = xcbr.Open();

        Assert.Null(cause);
    }

    [Fact]
    public void Close_WhenNotBlocked_ShouldReturnNull()
    {
        var (registry, xcbr) = CreateXcbr();

        var cause = xcbr.Close();

        Assert.Null(cause);
    }

    [Fact]
    public void Open_WhenLocIsTrue_ShouldReturnBlockedBySwitchingHierarchy()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Loc, true);

        var cause = xcbr.Open();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY, cause);
    }

    [Fact]
    public void Open_WhenBlkOpnIsTrue_ShouldReturnBlockedByInterlocking()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.BlkOpn, true);

        var cause = xcbr.Open();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING, cause);
    }

    [Fact]
    public void Close_WhenLocIsTrue_ShouldReturnBlockedBySwitchingHierarchy()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.Loc, true);

        var cause = xcbr.Close();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY, cause);
    }

    [Fact]
    public void Close_WhenBlkClsIsTrue_ShouldReturnBlockedByInterlocking()
    {
        var (registry, xcbr) = CreateXcbr();
        SetValue(registry, Refs.BlkCls, true);

        var cause = xcbr.Close();

        Assert.Equal(ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING, cause);
    }

    // ── Initialize defaults ───────────────────────────────────────────────

    [Fact]
    public void Initialize_WithDefaultCbOpCapValue_ShouldApplyFallback()
    {
        var (registry, xcbr) = CreateXcbrWithAttributes();

        xcbr.Initialize(_ => 0);  // cfg returns 0 (type default)

        Assert.Equal(4, registry.GetValue<object>(Refs.CbOpCap).Value);
    }

    [Fact]
    public void Initialize_WithNonDefaultCbOpCapValue_ShouldPreserveCfgValue()
    {
        var (registry, xcbr) = CreateXcbrWithAttributes();

        xcbr.Initialize(_ => 6);

        Assert.Equal(6, registry.GetValue<object>(Refs.CbOpCap).Value);
    }

    [Fact]
    public void Initialize_WithDefaultLocValue_ShouldApplyFallbackFalse()
    {
        var (registry, xcbr) = CreateXcbrWithAttributes();

        xcbr.Initialize(_ => false);

        // Default is false — value should still be set
        Assert.Equal(false, registry.GetValue<object>(Refs.Loc).Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static class Refs
    {
        public const string Pos = "LD/XCBR1.Pos.stVal";
        public const string Loc = "LD/XCBR1.Loc.stVal";
        public const string BlkOpn = "LD/XCBR1.BlkOpn.stVal";
        public const string BlkCls = "LD/XCBR1.BlkCls.stVal";
        public const string OpCnt = "LD/XCBR1.OpCnt.stVal";
        public const string CbOpCap = "LD/XCBR1.CBOpCap.stVal";
    }

    private static (PointRegistry, XCBR) CreateXcbr()
    {
        var registry = new PointRegistry();
        RegisterPoint(registry, Refs.Pos, "Pos");
        RegisterPoint(registry, Refs.Loc, "Loc");
        RegisterPoint(registry, Refs.BlkOpn, "BlkOpn");
        RegisterPoint(registry, Refs.BlkCls, "BlkCls");
        RegisterPoint(registry, Refs.OpCnt, "OpCnt");
        RegisterPoint(registry, Refs.CbOpCap, "CBOpCap");

        SetValue(registry, Refs.Loc, false);
        SetValue(registry, Refs.BlkOpn, false);
        SetValue(registry, Refs.BlkCls, false);
        SetValue(registry, Refs.OpCnt, 0);
        SetValue(registry, Refs.CbOpCap, 4);

        var xcbr = new XCBR("XCBR1", Refs.Pos, "Q01", registry,
            locRef: Refs.Loc,
            blkOpnRef: Refs.BlkOpn,
            blkClsRef: Refs.BlkCls,
            opCntRef: Refs.OpCnt,
            cbOpCapRef: Refs.CbOpCap);

        return (registry, xcbr);
    }

    /// <summary>Creates XCBR with proper DataAttribute objects needed for Initialize().</summary>
    private static (PointRegistry, XCBR) CreateXcbrWithAttributes()
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var xcbrLn = new LogicalNode("XCBR1", ld);

        var registry = new PointRegistry();

        var posRef = RegisterWithDa(registry, Refs.Pos, xcbrLn, "Pos", DataAttributeType.CODEDENUM, FunctionalConstraint.ST);
        var locRef = RegisterWithDa(registry, Refs.Loc, xcbrLn, "Loc", DataAttributeType.BOOLEAN, FunctionalConstraint.ST);
        var blkOpnRef = RegisterWithDa(registry, Refs.BlkOpn, xcbrLn, "BlkOpn", DataAttributeType.BOOLEAN, FunctionalConstraint.ST);
        var blkClsRef = RegisterWithDa(registry, Refs.BlkCls, xcbrLn, "BlkCls", DataAttributeType.BOOLEAN, FunctionalConstraint.ST);
        var opCntRef = RegisterWithDa(registry, Refs.OpCnt, xcbrLn, "OpCnt", DataAttributeType.INT32U, FunctionalConstraint.ST);
        var cbOpCapRef = RegisterWithDa(registry, Refs.CbOpCap, xcbrLn, "CBOpCap", DataAttributeType.INT32, FunctionalConstraint.ST);

        var xcbr = new XCBR("XCBR1", posRef, "Q01", registry,
            locRef: locRef,
            blkOpnRef: blkOpnRef,
            blkClsRef: blkClsRef,
            opCntRef: opCntRef,
            cbOpCapRef: cbOpCapRef);

        return (registry, xcbr);
    }

    private static void RegisterPoint(PointRegistry registry, string reference, string dataObject)
    {
        registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "",
            LogicalDevice = "LD",
            LogicalNode = "XCBR1",
            DataObject = dataObject,
            LnType = eLnType.XCBR,
            Fc = FunctionalConstraint.ST,
            ValueAttribute = (DataAttribute)RuntimeHelpers.GetUninitializedObject(typeof(DataAttribute))
        });
    }

    private static string RegisterWithDa(PointRegistry registry, string reference, LogicalNode ln,
        string dataObject, DataAttributeType daType, FunctionalConstraint fc)
    {
        var doNode = new DataObject(dataObject, ln);
        var da = new DataAttribute("stVal", doNode, daType, fc, TriggerOptions.NONE, 0);

        registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "",
            LogicalDevice = "LD",
            LogicalNode = "XCBR1",
            DataObject = dataObject,
            LnType = eLnType.XCBR,
            Fc = fc,
            ValueAttribute = da
        });

        return reference;
    }

    private static void SetValue(PointRegistry registry, string reference, object value)
        => registry.SetValue(new PointValue<object>
        {
            Reference = reference,
            Value = value,
            Quality = 192,
            Timestamp = DateTimeOffset.UtcNow
        });
}