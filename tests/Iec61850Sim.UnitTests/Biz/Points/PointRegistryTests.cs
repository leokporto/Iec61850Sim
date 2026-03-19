using IEC61850.Common;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.UnitTests.TestHelpers;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Points;

public class PointRegistryTests
{
    // ── Register / GetPoint ───────────────────────────────────────────────

    [Fact]
    public void Register_WithNewReference_ShouldBeAvailableByGetPoint()
    {
        var registry = new PointRegistry();
        var point = DevicePointFactory.CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST);

        registry.Register(point);
        var result = registry.GetPoint(point.Reference);

        Assert.Same(point, result);
    }

    [Fact]
    public void Register_WithSameReference_ShouldOverrideExistingPoint()
    {
        var registry = new PointRegistry();
        var first = DevicePointFactory.CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST);
        var second = DevicePointFactory.CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST,
            equipmentName: "Q02");

        registry.Register(first);
        registry.Register(second);
        var result = registry.GetPoint(second.Reference);

        Assert.Same(second, result);
        Assert.Single(registry.All);
    }

    [Fact]
    public void GetByFc_WithMixedPoints_ShouldReturnOnlyMatchingFc()
    {
        var registry = new PointRegistry();
        registry.Register(DevicePointFactory.CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST));
        registry.Register(DevicePointFactory.CreatePoint(
            reference: "LD0/MMXU1.PhV.phsA.cVal.mag.f",
            logicalNode: "MMXU1",
            dataObject: "PhV",
            lnType: eLnType.MMXU,
            fc: FunctionalConstraint.MX));

        var results = registry.GetByFc(FunctionalConstraint.ST).ToList();

        Assert.Single(results);
        Assert.Equal(FunctionalConstraint.ST, results[0].Fc);
    }

    // ── SetValue / GetValue (single) ──────────────────────────────────────

    [Fact]
    public void SetValue_WithRegisteredReference_ShouldMutateDevicePointInPlace()
    {
        var registry = new PointRegistry();
        var point = DevicePointFactory.CreatePoint(
            reference: "LD0/MMXU1.PhV.f",
            logicalNode: "MMXU1",
            dataObject: "PhV",
            lnType: eLnType.MMXU,
            fc: FunctionalConstraint.MX);
        registry.Register(point);
        var now = DateTimeOffset.UtcNow;

        registry.SetValue(new PointValue<double>
        {
            Reference = point.Reference,
            Value = 230.5,
            Timestamp = now,
            Quality = 192
        });

        Assert.Equal(230.5, point.Value);
        Assert.Equal(now, point.Timestamp);
        Assert.Equal((ushort)192, point.Quality);
    }

    [Fact]
    public void GetValue_AfterSetValue_ShouldReturnCurrentValue()
    {
        var registry = new PointRegistry();
        var point = DevicePointFactory.CreatePoint(
            reference: "LD0/MMXU1.PhV.f",
            logicalNode: "MMXU1",
            dataObject: "PhV",
            lnType: eLnType.MMXU,
            fc: FunctionalConstraint.MX);
        registry.Register(point);
        var now = DateTimeOffset.UtcNow;

        registry.SetValue(new PointValue<double>
        {
            Reference = point.Reference,
            Value = 110.0,
            Timestamp = now,
            Quality = 192
        });

        var result = registry.GetValue<double>(point.Reference);

        Assert.Equal(110.0, result.Value);
        Assert.Equal(now, result.Timestamp);
        Assert.Equal(FunctionalConstraint.MX, result.FC);
    }

    [Fact]
    public void SetValue_WithUnregisteredReference_ShouldThrowKeyNotFoundException()
    {
        var registry = new PointRegistry();

        Assert.Throws<KeyNotFoundException>(() =>
            registry.SetValue(new PointValue<object>
            {
                Reference = "LD0/UNKNOWN.Pos.stVal",
                Value = 1,
                Timestamp = DateTimeOffset.UtcNow
            }));
    }

    [Fact]
    public void GetValue_WithUnregisteredReference_ShouldThrowKeyNotFoundException()
    {
        var registry = new PointRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.GetValue<object>("LD0/UNKNOWN.Pos.stVal"));
    }

    // ── SetValues / GetValues (batch) ─────────────────────────────────────

    [Fact]
    public void SetValues_WithMultipleReferences_ShouldMutateAllDevicePoints()
    {
        var registry = new PointRegistry();
        var p1 = DevicePointFactory.CreatePoint("LD0/MMXU1.A.f", "MMXU1", "A", eLnType.MMXU, FunctionalConstraint.MX);
        var p2 = DevicePointFactory.CreatePoint("LD0/MMXU1.B.f", "MMXU1", "B", eLnType.MMXU, FunctionalConstraint.MX);
        registry.Register(p1);
        registry.Register(p2);
        var now = DateTimeOffset.UtcNow;

        registry.SetValues([
            new PointValue<object> { Reference = p1.Reference, Value = 1.0, Timestamp = now, Quality = 192 },
            new PointValue<object> { Reference = p2.Reference, Value = 2.0, Timestamp = now, Quality = 192 }
        ]);

        Assert.Equal(1.0, p1.Value);
        Assert.Equal(2.0, p2.Value);
    }

    [Fact]
    public void GetValues_WithMultipleReferences_ShouldReturnCurrentValues()
    {
        var registry = new PointRegistry();
        var p1 = DevicePointFactory.CreatePoint("LD0/MMXU1.A.f", "MMXU1", "A", eLnType.MMXU, FunctionalConstraint.MX);
        var p2 = DevicePointFactory.CreatePoint("LD0/MMXU1.B.f", "MMXU1", "B", eLnType.MMXU, FunctionalConstraint.MX);
        registry.Register(p1);
        registry.Register(p2);
        var now = DateTimeOffset.UtcNow;
        registry.SetValues([
            new PointValue<object> { Reference = p1.Reference, Value = 5.5, Timestamp = now, Quality = 192 },
            new PointValue<object> { Reference = p2.Reference, Value = 6.6, Timestamp = now, Quality = 192 }
        ]);

        var results = registry.GetValues([p1.Reference, p2.Reference]);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, v => v.Reference == p1.Reference && (double)v.Value! == 5.5);
        Assert.Contains(results, v => v.Reference == p2.Reference && (double)v.Value! == 6.6);
    }
}
