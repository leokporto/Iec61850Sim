using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Biz.Model;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Iec61850;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Model;

public class ModelScannerTests
{
    [Fact]
    public void Scan_WithUnknownLogicalDevice_ShouldNotRegisterPoints()
    {
        // Arrange
        var model = new IedModel("IED_TEST");
        var registry = new PointRegistry();
        var scanner = new ModelScanner();

        // Act
        scanner.Scan(model, registry, ["LD_DOES_NOT_EXIST"]);

        // Assert
        Assert.Empty(registry.All);
    }

    [Fact]
    public void Scan_WithLeafAttributes_ShouldRegisterClassifiedPoint()
    {
        // Arrange
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);

        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var quality = new DataAttribute("q", pos, DataAttributeType.QUALITY, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var timestamp = new DataAttribute("t", pos, DataAttributeType.TIMESTAMP, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var extra = new DataAttribute("origin", pos, DataAttributeType.BOOLEAN, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var scanner = new ModelScanner();

        // Act
        scanner.Scan(model, registry, ["LD0"]);
        var point = Assert.Single(registry.All);

        // Assert
        Assert.Equal("LD0", point.LogicalDevice);
        Assert.Equal("Q01XCBR1", point.LogicalNode);
        Assert.Equal("Pos", point.DataObject);
        Assert.Equal(eLnType.XCBR, point.LnType);
        Assert.Equal("Q01", point.EquipmentName);
        Assert.Equal(FunctionalConstraint.ST, point.Fc);
        Assert.NotNull(point.ValueAttribute);
        Assert.Equal("stVal", point.ValueAttribute.GetName());
        Assert.NotNull(point.QualityAttribute);
        Assert.Equal("q", point.QualityAttribute!.GetName());
        Assert.NotNull(point.TimestampAttribute);
        Assert.Equal("t", point.TimestampAttribute!.GetName());
        Assert.Contains(point.OtherAttributes, a => a.GetName() == "origin");
    }

    [Fact]
    public void Scan_WithSameDataObjectAndDifferentFc_ShouldRegisterSeparatedPoints()
    {
        // Arrange
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Obj1CSWI1", ld);
        var pos = new DataObject("Pos", ln);

        var ctlVal = new DataAttribute("ctlVal", pos, DataAttributeType.BOOLEAN, FunctionalConstraint.CO, TriggerOptions.NONE, 0);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var scanner = new ModelScanner();

        // Act
        scanner.Scan(model, registry, ["LD0"]);
        var points = registry.All.ToList();

        // Assert
        Assert.Equal(2, points.Count);
        Assert.Contains(points, p => p.Fc == FunctionalConstraint.CO && p.ValueAttribute.GetName() == "ctlVal");
        Assert.Contains(points, p => p.Fc == FunctionalConstraint.ST && p.ValueAttribute.GetName() == "stVal");
        Assert.All(points, p => Assert.Equal(eLnType.CSWI, p.LnType));
        Assert.All(points, p => Assert.Equal("Obj1", p.EquipmentName));
    }
}
