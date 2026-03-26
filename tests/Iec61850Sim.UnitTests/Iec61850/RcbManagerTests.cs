using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Xunit;

namespace Iec61850Sim.UnitTests.Iec61850;

public class RcbManagerTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a minimal IedModel containing a single DataAttribute reachable at
    /// <paramref name="shortRef"/> (dot notation, no IED-name prefix).
    /// </summary>
    private static (IedModel model, DataAttribute da) BuildModel(string shortRef)
    {
        // shortRef format: "LDName/LNName.DOName.DAName"
        var parts  = shortRef.Split('/');
        var ldName = parts[0];
        var rest   = parts[1].Split('.');
        var lnName = rest[0];
        var doName = rest[1];
        var daName = rest[2];

        var model = new IedModel("IED_TEST");
        var ld    = new LogicalDevice(ldName, model);
        var ln    = new LogicalNode(lnName, ld);
        var doObj = new DataObject(doName, ln);
        var da    = new DataAttribute(daName, doObj, DataAttributeType.FLOAT32,
                        FunctionalConstraint.MX, TriggerOptions.NONE, 0);

        return (model, da);
    }

    /// <summary>
    /// Creates a DevicePoint with a non-null ValueAttribute so Initialize does not skip it.
    /// </summary>
    private static DevicePoint PointFor(string reference, DataAttribute va) => new()
    {
        Reference      = reference,
        EquipmentName  = "EQ",
        LogicalDevice  = reference.Split('/')[0],
        LogicalNode    = reference.Split('/')[1].Split('.')[0],
        DataObject     = reference.Split('/')[1].Split('.')[1],
        LnType         = eLnType.MMXU,
        Fc             = FunctionalConstraint.MX,
        ValueAttribute = va
    };

    /// <summary>
    /// Creates a ReportControlBlock attached to a freshly built model hierarchy.
    /// Note: rcb.DataSet is read-only from the native binding and is only populated
    /// by the IedServer at runtime; its value cannot be set in unit tests.
    /// </summary>
    private static ReportControlBlock BuildRcb(string ldName, string lnName, string rcbName)
    {
        var model = new IedModel("IED_TEST");
        var ld    = new LogicalDevice(ldName, model);
        var ln    = new LogicalNode(lnName, ld);

        // Constructor: (name, parent, dataSetName, buffered, rptId, trgOps, optFlds, bufTm, intgPd, confRev)
        return new ReportControlBlock(rcbName, ln,
            string.Empty,
            /*buffered*/   false,
            /*rptId*/      string.Empty,
            /*trgOps*/     0u,
            /*optFlds*/    (byte)0,
            /*bufTm*/      (byte)0,
            /*intgPd*/     0u,
            /*confRev*/    0u);
    }

    // -------------------------------------------------------------------------
    // Initialize — DataSet member handle resolution
    // -------------------------------------------------------------------------

    [Fact]
    public void Initialize_WithMatchingReference_CachesHandle()
    {
        const string @ref = "LD0/MMXU1.PhV.f";
        var (model, da)   = BuildModel(@ref);
        var registry      = new PointRegistry();
        registry.Register(PointFor(@ref, da));

        var mgr = new RcbManager();
        mgr.Initialize(model, registry);

        // Handle must be cached (not null) for the registered reference
        Assert.NotNull(mgr.GetMemberHandle(@ref));
    }

    [Fact]
    public void Initialize_WithNonMatchingReference_DoesNotCacheHandle()
    {
        const string modelRef    = "LD0/MMXU1.PhV.f";
        const string registryRef = "LD0/MMXU1.PhV.g";   // attribute absent from model

        var (model, da) = BuildModel(modelRef);
        var registry    = new PointRegistry();
        registry.Register(PointFor(registryRef, da));

        var mgr = new RcbManager();
        mgr.Initialize(model, registry);

        Assert.Null(mgr.GetMemberHandle(registryRef));
    }

    [Fact]
    public void Initialize_SkipsPointsWithNullValueAttribute()
    {
        const string @ref = "LD0/MMXU1.PhV.f";
        var (model, _)    = BuildModel(@ref);
        var registry      = new PointRegistry();

        // DevicePoint with null ValueAttribute — Initialize must skip it without throwing
        var point = new DevicePoint
        {
            Reference      = @ref,
            EquipmentName  = "EQ",
            LogicalDevice  = "LD0",
            LogicalNode    = "MMXU1",
            DataObject     = "PhV",
            LnType         = eLnType.MMXU,
            Fc             = FunctionalConstraint.MX,
            ValueAttribute = null!
        };
        registry.Register(point);

        var mgr = new RcbManager();
        mgr.Initialize(model, registry);   // must not throw

        Assert.Null(mgr.GetMemberHandle(@ref));
    }

    [Fact]
    public void Initialize_WithMultiplePoints_CachesAllMatchingHandles()
    {
        const string ref1 = "LD0/MMXU1.PhV.f";
        const string ref2 = "LD0/MMXU1.Hz.f";

        var model = new IedModel("IED_TEST");
        var ld    = new LogicalDevice("LD0", model);
        var ln    = new LogicalNode("MMXU1", ld);
        var do1   = new DataObject("PhV", ln);
        var da1   = new DataAttribute("f", do1, DataAttributeType.FLOAT32, FunctionalConstraint.MX, TriggerOptions.NONE, 0);
        var do2   = new DataObject("Hz", ln);
        var da2   = new DataAttribute("f", do2, DataAttributeType.FLOAT32, FunctionalConstraint.MX, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(PointFor(ref1, da1));
        registry.Register(PointFor(ref2, da2));

        var mgr = new RcbManager();
        mgr.Initialize(model, registry);

        Assert.NotNull(mgr.GetMemberHandle(ref1));
        Assert.NotNull(mgr.GetMemberHandle(ref2));
    }

    // -------------------------------------------------------------------------
    // GetMemberHandle — dictionary look-up
    // -------------------------------------------------------------------------

    [Fact]
    public void GetMemberHandle_UnknownReference_ReturnsNull()
    {
        var mgr = new RcbManager();

        Assert.Null(mgr.GetMemberHandle("LD0/MMXU1.PhV.f"));
    }

    [Fact]
    public void GetMemberHandle_AfterInitialize_ReturnsCachedHandle()
    {
        const string @ref = "LD0/MMXU1.PhV.f";
        var (model, da)   = BuildModel(@ref);
        var registry      = new PointRegistry();
        registry.Register(PointFor(@ref, da));

        var mgr = new RcbManager();
        mgr.Initialize(model, registry);

        // Calling GetMemberHandle twice must return a handle each time (idempotent)
        Assert.NotNull(mgr.GetMemberHandle(@ref));
        Assert.NotNull(mgr.GetMemberHandle(@ref));
    }

    // -------------------------------------------------------------------------
    // EnsureCached + RcbByKey
    // -------------------------------------------------------------------------

    [Fact]
    public void EnsureCached_NewRcb_AddsToRcbByKey()
    {
        var mgr = new RcbManager();
        var rcb = BuildRcb("LD0", "CSWI1", "urcb01");

        mgr.EnsureCached(rcb);

        Assert.True(mgr.RcbByKey.ContainsKey("LD0/CSWI1/urcb01"));
    }

    [Fact]
    public void EnsureCached_StoresCorrectRcbInstance()
    {
        var mgr = new RcbManager();
        var rcb = BuildRcb("LD0", "CSWI1", "urcb01");

        mgr.EnsureCached(rcb);

        Assert.Same(rcb, mgr.RcbByKey["LD0/CSWI1/urcb01"]);
    }

    [Fact]
    public void EnsureCached_SameRcbTwice_OnlyAddsOnce()
    {
        var mgr = new RcbManager();
        var rcb = BuildRcb("LD0", "CSWI1", "urcb01");

        mgr.EnsureCached(rcb);
        mgr.EnsureCached(rcb);

        Assert.Single(mgr.RcbByKey);
    }

    [Fact]
    public void EnsureCached_TwoDistinctRcbs_BothCached()
    {
        var mgr  = new RcbManager();
        var rcb1 = BuildRcb("LD0", "CSWI1", "urcb01");
        var rcb2 = BuildRcb("LD0", "CSWI1", "urcb02");

        mgr.EnsureCached(rcb1);
        mgr.EnsureCached(rcb2);

        Assert.Equal(2, mgr.RcbByKey.Count);
        Assert.True(mgr.RcbByKey.ContainsKey("LD0/CSWI1/urcb01"));
        Assert.True(mgr.RcbByKey.ContainsKey("LD0/CSWI1/urcb02"));
    }

    [Fact]
    public void EnsureCached_KeyIncludesLdAndLnAndRcbName()
    {
        var mgr = new RcbManager();
        var rcb = BuildRcb("Measurement", "MMXU1", "urcb01");

        mgr.EnsureCached(rcb);

        // Key format must be "{ldName}/{lnName}/{rcbName}"
        Assert.True(mgr.RcbByKey.ContainsKey("Measurement/MMXU1/urcb01"));
    }

    // -------------------------------------------------------------------------
    // GetDataSetLd
    // -------------------------------------------------------------------------

    [Fact]
    public void GetDataSetLd_WhenRcbNotCached_ReturnsNull()
    {
        var mgr = new RcbManager();
        // Use any RCB — EnsureCached has never been called
        var rcb = BuildRcb("LD0", "CSWI1", "urcb01");

        Assert.Null(mgr.GetDataSetLd(rcb));
    }

    [Fact]
    public void GetDataSetLd_WhenRcbHasEmptyDataSet_ReturnsNull()
    {
        // rcb.DataSet is read-only from the native binding and empty when constructed
        // in a unit test (server sets it at runtime). This verifies the empty-DataSet path.
        var mgr = new RcbManager();
        var rcb = BuildRcb("LD0", "CSWI1", "urcb01");

        mgr.EnsureCached(rcb);

        Assert.Null(mgr.GetDataSetLd(rcb));
    }
}