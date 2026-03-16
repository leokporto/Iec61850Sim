using IEC61850.Common;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.UnitTests.TestHelpers;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Points;

public class PointRegistryTests
{
    [Fact]
    public void Register_WithNewReference_ShouldBeAvailableByGet()
    {
        // Arrange
        var registry = new PointRegistry();
        var point = DevicePointFactory.CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST);

        // Act
        registry.Register(point);
        var result = registry.Get(point.Reference);

        // Assert
        Assert.Same(point, result);
    }

    [Fact]
    public void Register_WithSameReference_ShouldOverrideExistingPoint()
    {
        // Arrange
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

        // Act
        registry.Register(second);
        var result = registry.Get(second.Reference);

        // Assert
        Assert.Same(second, result);
        Assert.Single(registry.All);
    }

    [Fact]
    public void GetByFc_WithMixedPoints_ShouldReturnOnlyMatchingFc()
    {
        // Arrange
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

        // Act
        var results = registry.GetByFc(FunctionalConstraint.ST).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(FunctionalConstraint.ST, results[0].Fc);
    }
}
