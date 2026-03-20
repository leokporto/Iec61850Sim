using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Model.Tree;
using Iec61850Sim.Core.Points;
using Xunit;

namespace Iec61850Sim.UnitTests.Model.Tree;

public class ModelTreeBuilderTests
{
    private readonly ModelTreeBuilder _sut = new();

    [Fact]
    public void Build_WithEmptyRegistry_ShouldReturnIedNodeWithNoChildren()
    {
        var model = new IedModel("IED_TEST");
        var registry = new PointRegistry();

        var tree = _sut.Build("IED_TEST", model, registry);

        Assert.IsType<IedNode>(tree);
        Assert.Equal("IED_TEST", tree.Name);
        Assert.Empty(tree.Children);
    }

    [Fact]
    public void Build_WithSinglePoint_ShouldBuildFullHierarchy()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var point = CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST);
        registry.Register(point);

        var tree = _sut.Build("IED_TEST", model, registry);

        var ldNode = Assert.Single(tree.Children);
        Assert.IsType<LogicalDeviceNode>(ldNode);
        Assert.Equal("LD0", ldNode.Name);

        var lnNode = Assert.Single(ldNode.Children);
        Assert.IsType<LogicalNodeNode>(lnNode);
        Assert.Equal("Q01XCBR1", lnNode.Name);

        var doNode = Assert.Single(lnNode.Children);
        Assert.IsType<DataObjectNode>(doNode);
        Assert.Equal("Pos", doNode.Name);

        var daNode = Assert.Single(doNode.Children);
        var typedDaNode = Assert.IsType<DataAttributeNode>(daNode);
        Assert.Equal(point.Reference, typedDaNode.Point.Reference);
        Assert.Equal(point.Fc, typedDaNode.Point.FC);
    }

    [Fact]
    public void Build_WithSdoHierarchy_ShouldBuildNestedDataObjectNodes()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("MMXU1", ld);
        var phv = new DataObject("PhV", ln);
        var phsA = new DataObject("phsA", phv);   // SDO
        var cVal = new DataObject("cVal", phsA);   // SDO
        var mag = new DataObject("mag", cVal);     // SDO (innermost)
        var f = new DataAttribute("f", mag, DataAttributeType.FLOAT32, FunctionalConstraint.MX, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var point = CreatePoint(f.GetObjectReference(), FunctionalConstraint.MX);
        registry.Register(point);

        var tree = _sut.Build("IED_TEST", model, registry);

        var ldNode = Assert.Single(tree.Children);
        var lnNode = Assert.Single(ldNode.Children);

        var phvNode = Assert.Single(lnNode.Children);
        Assert.Equal("PhV", phvNode.Name);

        var phsANode = Assert.Single(phvNode.Children);
        Assert.Equal("phsA", phsANode.Name);

        var cValNode = Assert.Single(phsANode.Children);
        Assert.Equal("cVal", cValNode.Name);

        var magNode = Assert.Single(cValNode.Children);
        Assert.Equal("mag", magNode.Name);

        var daNode = Assert.Single(magNode.Children);
        Assert.IsType<DataAttributeNode>(daNode);
    }

    [Fact]
    public void Build_WithTwoPointsInSameDoUnderDifferentFc_ShouldCreateTwoDaNodes()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Obj1CSWI1", ld);
        var pos = new DataObject("Pos", ln);
        var ctlVal = new DataAttribute("ctlVal", pos, DataAttributeType.BOOLEAN, FunctionalConstraint.CO, TriggerOptions.NONE, 0);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var coPoint = CreatePoint(ctlVal.GetObjectReference(), FunctionalConstraint.CO);
        var stPoint = CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST);
        registry.Register(coPoint);
        registry.Register(stPoint);

        var tree = _sut.Build("IED_TEST", model, registry);

        var doNode = tree.Children[0].Children[0].Children[0];
        Assert.Equal(2, doNode.Children.Count);
        Assert.Contains(doNode.Children, n => Assert.IsType<DataAttributeNode>(n).Point.Reference == coPoint.Reference);
        Assert.Contains(doNode.Children, n => Assert.IsType<DataAttributeNode>(n).Point.Reference == stPoint.Reference);
    }

    [Fact]
    public void Build_WithQualityAndTimestampAttributes_ShouldNotDuplicateDevicePoint()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var q = new DataAttribute("q", pos, DataAttributeType.QUALITY, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var t = new DataAttribute("t", pos, DataAttributeType.TIMESTAMP, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var point = CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST);
        registry.Register(point);

        var tree = _sut.Build("IED_TEST", model, registry);

        var doNode = tree.Children[0].Children[0].Children[0];
        var daNode = Assert.Single(doNode.Children);
        Assert.Equal(point.Reference, Assert.IsType<DataAttributeNode>(daNode).Point.Reference);
    }

    [Fact]
    public void Build_IedNode_GetPoints_ShouldReturnEmpty()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST));

        var tree = _sut.Build("IED_TEST", model, registry);

        Assert.Empty(tree.GetPoints());
    }

    [Fact]
    public void Build_LogicalDeviceNode_GetPoints_ShouldReturnEmpty()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST));

        var tree = _sut.Build("IED_TEST", model, registry);

        var ldNode = tree.Children[0];
        Assert.Empty(ldNode.GetPoints());
    }

    [Fact]
    public void Build_LogicalNodeNode_GetPoints_ShouldReturnAllDescendantPoints()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var ctlVal = new DataAttribute("ctlVal", pos, DataAttributeType.BOOLEAN, FunctionalConstraint.CO, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var stPoint = CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST);
        var coPoint = CreatePoint(ctlVal.GetObjectReference(), FunctionalConstraint.CO);
        registry.Register(stPoint);
        registry.Register(coPoint);

        var tree = _sut.Build("IED_TEST", model, registry);

        var lnNode = tree.Children[0].Children[0];
        var points = lnNode.GetPoints().ToList();

        Assert.Equal(2, points.Count);
        Assert.Contains(points, pv => pv.Reference == stPoint.Reference);
        Assert.Contains(points, pv => pv.Reference == coPoint.Reference);
    }

    [Fact]
    public void Build_DataObjectNode_GetPoints_ShouldReturnOwnLeafPoints()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var point = CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST);
        registry.Register(point);

        var tree = _sut.Build("IED_TEST", model, registry);

        var doNode = tree.Children[0].Children[0].Children[0];
        var points = doNode.GetPoints().ToList();

        Assert.Single(points);
        Assert.Equal(point.Reference, points[0].Reference);
    }

    [Fact]
    public void Build_DataAttributeNode_GetPoints_ShouldReturnOwnPointValue()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("Q01XCBR1", ld);
        var pos = new DataObject("Pos", ln);
        var stVal = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var point = CreatePoint(stVal.GetObjectReference(), FunctionalConstraint.ST);
        registry.Register(point);

        var tree = _sut.Build("IED_TEST", model, registry);

        var daNode = Assert.IsType<DataAttributeNode>(tree.Children[0].Children[0].Children[0].Children[0]);

        var pv = Assert.Single(daNode.GetPoints());
        Assert.Equal(point.Reference, pv.Reference);
        Assert.Equal(point.Fc, pv.FC);
        Assert.Same(daNode.Point, pv);
    }

    [Fact]
    public void Build_GetLivePoints_ShouldRefreshValueFromRegistry()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);
        var ln = new LogicalNode("MMXU1", ld);
        var phv = new DataObject("PhV", ln);
        var f = new DataAttribute("f", phv, DataAttributeType.FLOAT32, FunctionalConstraint.MX, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var point = CreatePoint(f.GetObjectReference(), FunctionalConstraint.MX);
        registry.Register(point);

        var tree = _sut.Build("IED_TEST", model, registry);

        // Simulate a value update via registry
        var now = DateTimeOffset.UtcNow;
        registry.SetValue(new PointValue<object> { Reference = point.Reference, Value = 42.5, Timestamp = now, Quality = 192 });

        var livePoints = tree.GetLivePoints(registry).ToList();

        Assert.Single(livePoints);
        Assert.Equal(42.5, livePoints[0].Value);
        Assert.Equal(now, livePoints[0].Timestamp);
    }

    [Fact]
    public void Build_WithUnknownLogicalDevice_ShouldSkipLd()
    {
        var model = new IedModel("IED_TEST");
        var registry = new PointRegistry();
        registry.Register(CreatePoint("LD_OTHER/LN1.DO1.stVal", FunctionalConstraint.ST, "LD_OTHER"));

        var tree = _sut.Build("IED_TEST", model, registry);

        Assert.Empty(tree.Children);
    }

    private static DevicePoint CreatePoint(
        string reference,
        FunctionalConstraint fc,
        string logicalDevice = "LD0")
    {
        return new DevicePoint
        {
            Reference = reference,
            LogicalDevice = logicalDevice,
            LogicalNode = "LN1",
            DataObject = "DO1",
            LnType = eLnType.Unknown,
            EquipmentName = "",
            Fc = fc,
            ValueAttribute = null!
        };
    }
}