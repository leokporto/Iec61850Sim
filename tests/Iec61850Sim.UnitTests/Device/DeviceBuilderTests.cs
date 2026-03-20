using System.Runtime.CompilerServices;
using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Xunit;

namespace Iec61850Sim.UnitTests.Device;

public class DeviceBuilderTests
{
    [Fact]
    public void Build_WithValidXcbrStPosPoint_ShouldRegisterBreaker()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();
        var xcbrPos = CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            equipmentName: "Q01",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true);

        registry.Register(xcbrPos);

        // Act
        sut.Build(registry, manager);

        // Assert
        Assert.Single(manager.Breakers);
        Assert.Equal("XCBR1", manager.Breakers[0].Name);
        Assert.Equal("LD0/XCBR1.Pos.stVal", manager.Breakers[0].PositionReference);
        Assert.Equal("Q01", manager.Breakers[0].EquipmentName);
    }

    [Fact]
    public void Build_WithMmxuMxPointsGroupedByLogicalNode_ShouldRegisterMeasurementPerGroup()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();

        registry.Register(CreatePoint(
            reference: "LD0/MMXU1.PhV.phsA.cVal.mag.f",
            equipmentName: "MMXU_EQ_1",
            logicalNode: "MMXU1",
            dataObject: "PhV",
            lnType: eLnType.MMXU,
            fc: FunctionalConstraint.MX,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/MMXU1.PhV.phsB.cVal.mag.f",
            equipmentName: "MMXU_EQ_1",
            logicalNode: "MMXU1",
            dataObject: "PhV",
            lnType: eLnType.MMXU,
            fc: FunctionalConstraint.MX,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/MMXU2.PhV.phsC.cVal.mag.f",
            equipmentName: "MMXU_EQ_2",
            logicalNode: "MMXU2",
            dataObject: "PhV",
            lnType: eLnType.MMXU,
            fc: FunctionalConstraint.MX,
            hasValueAttribute: true));

        // Act
        sut.Build(registry, manager);

        // Assert
        Assert.Equal(2, manager.Measurements.Count);
        Assert.Contains(manager.Measurements, x => x.Name == "MMXU1");
        Assert.Contains(manager.Measurements, x => x.Name == "MMXU2");
    }

    [Fact]
    public void Build_WithMatchingCswiAndBreaker_ShouldRegisterController()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();

        registry.Register(CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            equipmentName: "Q01",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/CSWI1.Pos.Oper.ctlVal",
            equipmentName: "Q01",
            logicalNode: "CSWI1",
            dataObject: "Pos",
            lnType: eLnType.CSWI,
            fc: FunctionalConstraint.CO,
            hasValueAttribute: false));

        registry.Register(CreatePoint(
            reference: "LD0/CSWI1.Pos.stVal",
            equipmentName: "Q01",
            logicalNode: "CSWI1",
            dataObject: "Pos",
            lnType: eLnType.CSWI,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        // Act
        sut.Build(registry, manager);

        // Assert
        Assert.Single(manager.Breakers);
        Assert.Single(manager.Controllers);
        Assert.Same(manager.Breakers[0], manager.Controllers[0].Xcbr);
        Assert.Equal("LD0/CSWI1.Pos.Oper.ctlVal", manager.Controllers[0].CommandReference);
        Assert.Equal("LD0/CSWI1.Pos.stVal", manager.Controllers[0].PositionReference);
    }

    [Fact]
    public void Build_WithCswiWithoutPosPoint_ShouldNotRegisterController()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();

        registry.Register(CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            equipmentName: "Q01",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/CSWI1.Pos.Oper.ctlVal",
            equipmentName: "Q01",
            logicalNode: "CSWI1",
            dataObject: "Pos",
            lnType: eLnType.CSWI,
            fc: FunctionalConstraint.CO,
            hasValueAttribute: false));

        // Act
        sut.Build(registry, manager);

        // Assert
        Assert.Empty(manager.Controllers);
    }

    [Fact]
    public void Build_WithCswiAndNoMatchingBreaker_ShouldNotRegisterController()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();

        registry.Register(CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            equipmentName: "Q01",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/CSWI1.Pos.Oper.ctlVal",
            equipmentName: "Q02",
            logicalNode: "CSWI1",
            dataObject: "Pos",
            lnType: eLnType.CSWI,
            fc: FunctionalConstraint.CO,
            hasValueAttribute: false));

        registry.Register(CreatePoint(
            reference: "LD0/CSWI1.Pos.stVal",
            equipmentName: "Q02",
            logicalNode: "CSWI1",
            dataObject: "Pos",
            lnType: eLnType.CSWI,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        // Act
        sut.Build(registry, manager);

        // Assert
        Assert.Empty(manager.Controllers);
    }

    [Fact]
    public void Build_WithLLN0Points_ShouldRegisterLLN0DevicePerLogicalDevice()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();

        registry.Register(CreatePoint(
            reference: "LD0/LLN0.Mod.stVal",
            equipmentName: "",
            logicalNode: "LLN0",
            dataObject: "Mod",
            lnType: eLnType.LLN0,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/LLN0.NamPlt.vendor",
            equipmentName: "",
            logicalNode: "LLN0",
            dataObject: "NamPlt",
            lnType: eLnType.LLN0,
            fc: FunctionalConstraint.DC,
            hasValueAttribute: true));

        // Act
        sut.Build(registry, manager);

        // Assert
        Assert.Single(manager.LLN0Devices);
        Assert.Equal("LLN0_LD0", manager.LLN0Devices[0].Name);
    }

    [Fact]
    public void Build_WithLPHDPoints_ShouldRegisterLPHDDevicePerLogicalDevice()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();

        registry.Register(CreatePoint(
            reference: "LD0/LPHD1.PhyHealth.stVal",
            equipmentName: "",
            logicalNode: "LPHD1",
            dataObject: "PhyHealth",
            lnType: eLnType.LPHD,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/LPHD1.PhyNam.vendor",
            equipmentName: "",
            logicalNode: "LPHD1",
            dataObject: "PhyNam",
            lnType: eLnType.LPHD,
            fc: FunctionalConstraint.DC,
            hasValueAttribute: true));

        // Act
        sut.Build(registry, manager);

        // Assert
        Assert.Single(manager.LPHDDevices);
        Assert.Equal("LPHD_LD0", manager.LPHDDevices[0].Name);
    }

    [Fact]
    public void Build_WithDuplicateXcbrStPosPointsInSameLogicalNode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var sut = new DeviceBuilder();

        registry.Register(CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal",
            equipmentName: "Q01",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        registry.Register(CreatePoint(
            reference: "LD0/XCBR1.Pos.stVal.alt",
            equipmentName: "Q01",
            logicalNode: "XCBR1",
            dataObject: "Pos",
            lnType: eLnType.XCBR,
            fc: FunctionalConstraint.ST,
            hasValueAttribute: true));

        // Act
        var action = () => sut.Build(registry, manager);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    private static DevicePoint CreatePoint(
        string reference,
        string equipmentName,
        string logicalNode,
        string dataObject,
        eLnType lnType,
        FunctionalConstraint fc,
        bool hasValueAttribute)
    {
        return new DevicePoint
        {
            Reference = reference,
            EquipmentName = equipmentName,
            LogicalDevice = "LD0",
            LogicalNode = logicalNode,
            DataObject = dataObject,
            LnType = lnType,
            Fc = fc,
            ValueAttribute = hasValueAttribute ? CreateDataAttribute() : null!
        };
    }

    private static DataAttribute CreateDataAttribute()
    {
        return (DataAttribute)RuntimeHelpers.GetUninitializedObject(typeof(DataAttribute));
    }
}