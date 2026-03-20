using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Xunit;

namespace Iec61850Sim.UnitTests.Device;

public class Lln0Tests
{
    [Fact]
    public void Initialize_WithEmptyStringCfgValue_ShouldApplyDefaultVendor()
    {
        var (registry, vendorDa) = CreateStringPoint("LD/LLN0.NamPlt.vendor", "NamPlt");
        var device = new LLN0("LD", ["LD/LLN0.NamPlt.vendor"], registry);

        device.Initialize(_ => null);

        Assert.Equal("IEC61850Sim", registry.GetValue<object>("LD/LLN0.NamPlt.vendor").Value);
    }

    [Fact]
    public void Initialize_WithNonEmptyStringCfgValue_ShouldPreserveCfgValue()
    {
        var (registry, _) = CreateStringPoint("LD/LLN0.NamPlt.vendor", "NamPlt");
        var device = new LLN0("LD", ["LD/LLN0.NamPlt.vendor"], registry);

        device.Initialize(_ => "CustomVendor");

        Assert.Equal("CustomVendor", registry.GetValue<object>("LD/LLN0.NamPlt.vendor").Value);
    }

    [Fact]
    public void Initialize_WithZeroIntCfgValue_ShouldApplyDefaultStVal()
    {
        var (registry, _) = CreateIntPoint("LD/LLN0.Mod.stVal", "Mod");
        var device = new LLN0("LD", ["LD/LLN0.Mod.stVal"], registry);

        device.Initialize(_ => 0);

        Assert.Equal(1, registry.GetValue<object>("LD/LLN0.Mod.stVal").Value);
    }

    [Fact]
    public void Initialize_WithNonZeroIntCfgValue_ShouldPreserveCfgValue()
    {
        var (registry, _) = CreateIntPoint("LD/LLN0.Mod.stVal", "Mod");
        var device = new LLN0("LD", ["LD/LLN0.Mod.stVal"], registry);

        device.Initialize(_ => 3);

        Assert.Equal(3, registry.GetValue<object>("LD/LLN0.Mod.stVal").Value);
    }

    [Fact]
    public void Initialize_WithNullCfgValueAndNoDefault_ShouldNotSetRegistryValue()
    {
        // "UnknownDO.stVal" has no entry in Defaults, cfg returns null → skip
        var (registry, _) = CreateIntPoint("LD/LLN0.UnknownDO.stVal", "UnknownDO");
        var device = new LLN0("LD", ["LD/LLN0.UnknownDO.stVal"], registry);

        device.Initialize(_ => null);

        Assert.Null(registry.GetValue<object>("LD/LLN0.UnknownDO.stVal").Value);
    }

    [Fact]
    public void Step_ShouldNotMutateRegistry()
    {
        var (registry, _) = CreateIntPoint("LD/LLN0.Mod.stVal", "Mod");
        registry.SetValue(new PointValue<object> { Reference = "LD/LLN0.Mod.stVal", Value = 1, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
        var device = new LLN0("LD", ["LD/LLN0.Mod.stVal"], registry);

        device.Step(0.1);

        Assert.Equal(1, registry.GetValue<object>("LD/LLN0.Mod.stVal").Value);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static (PointRegistry, DataAttribute) CreateStringPoint(string reference, string dataObject)
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var lln0 = new LogicalNode("LLN0", ld);
        var doNode = new DataObject(dataObject, lln0);
        var daName = reference.Split('.').Last();
        var da = new DataAttribute(daName, doNode, DataAttributeType.VISIBLE_STRING_32, FunctionalConstraint.DC, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "",
            LogicalDevice = "LD",
            LogicalNode = "LLN0",
            DataObject = dataObject,
            LnType = eLnType.LLN0,
            Fc = FunctionalConstraint.DC,
            ValueAttribute = da
        });

        return (registry, da);
    }

    private static (PointRegistry, DataAttribute) CreateIntPoint(string reference, string dataObject)
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var lln0 = new LogicalNode("LLN0", ld);
        var doNode = new DataObject(dataObject, lln0);
        var da = new DataAttribute("stVal", doNode, DataAttributeType.INT32, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "",
            LogicalDevice = "LD",
            LogicalNode = "LLN0",
            DataObject = dataObject,
            LnType = eLnType.LLN0,
            Fc = FunctionalConstraint.ST,
            ValueAttribute = da
        });

        return (registry, da);
    }
}