using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Xunit;

namespace Iec61850Sim.UnitTests.Device;

public class LphdTests
{
    [Fact]
    public void Initialize_WithEmptyStringCfgValue_ShouldApplyDefaultVendor()
    {
        var (registry, _) = CreateStringPoint("LD/LPHD1.PhyNam.vendor", "PhyNam");
        var device = new LPHD("LD", ["LD/LPHD1.PhyNam.vendor"], registry);

        device.Initialize(_ => null);

        Assert.Equal("IEC61850Sim", registry.GetValue<object>("LD/LPHD1.PhyNam.vendor").Value);
    }

    [Fact]
    public void Initialize_WithNonEmptyStringCfgValue_ShouldPreserveCfgValue()
    {
        var (registry, _) = CreateStringPoint("LD/LPHD1.PhyNam.vendor", "PhyNam");
        var device = new LPHD("LD", ["LD/LPHD1.PhyNam.vendor"], registry);

        device.Initialize(_ => "AcmeCorp");

        Assert.Equal("AcmeCorp", registry.GetValue<object>("LD/LPHD1.PhyNam.vendor").Value);
    }

    [Fact]
    public void Initialize_WithZeroIntCfgValue_ShouldApplyDefaultPhyHealthStVal()
    {
        var (registry, _) = CreateIntPoint("LD/LPHD1.PhyHealth.stVal", "PhyHealth");
        var device = new LPHD("LD", ["LD/LPHD1.PhyHealth.stVal"], registry);

        device.Initialize(_ => 0);

        Assert.Equal(1, registry.GetValue<object>("LD/LPHD1.PhyHealth.stVal").Value);
    }

    [Fact]
    public void Initialize_WithFalseBoolCfgValue_ShouldApplyDefaultProxyStVal()
    {
        var (registry, _) = CreateBoolPoint("LD/LPHD1.Proxy.stVal", "Proxy");
        var device = new LPHD("LD", ["LD/LPHD1.Proxy.stVal"], registry);

        device.Initialize(_ => false);

        // Default for Proxy.stVal is false — value is set (not skipped)
        Assert.Equal(false, registry.GetValue<object>("LD/LPHD1.Proxy.stVal").Value);
    }

    [Fact]
    public void Step_ShouldNotMutateRegistry()
    {
        var (registry, _) = CreateIntPoint("LD/LPHD1.PhyHealth.stVal", "PhyHealth");
        registry.SetValue(new PointValue<object> { Reference = "LD/LPHD1.PhyHealth.stVal", Value = 1, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
        var device = new LPHD("LD", ["LD/LPHD1.PhyHealth.stVal"], registry);

        device.Step(0.1);

        Assert.Equal(1, registry.GetValue<object>("LD/LPHD1.PhyHealth.stVal").Value);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static (PointRegistry, DataAttribute) CreateStringPoint(string reference, string dataObject)
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var lphd = new LogicalNode("LPHD1", ld);
        var doNode = new DataObject(dataObject, lphd);
        var daName = reference.Split('.').Last();
        var da = new DataAttribute(daName, doNode, DataAttributeType.VISIBLE_STRING_32, FunctionalConstraint.DC, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "",
            LogicalDevice = "LD",
            LogicalNode = "LPHD1",
            DataObject = dataObject,
            LnType = eLnType.LPHD,
            Fc = FunctionalConstraint.DC,
            ValueAttribute = da
        });

        return (registry, da);
    }

    private static (PointRegistry, DataAttribute) CreateIntPoint(string reference, string dataObject)
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var lphd = new LogicalNode("LPHD1", ld);
        var doNode = new DataObject(dataObject, lphd);
        var da = new DataAttribute("stVal", doNode, DataAttributeType.INT32, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "",
            LogicalDevice = "LD",
            LogicalNode = "LPHD1",
            DataObject = dataObject,
            LnType = eLnType.LPHD,
            Fc = FunctionalConstraint.ST,
            ValueAttribute = da
        });

        return (registry, da);
    }

    private static (PointRegistry, DataAttribute) CreateBoolPoint(string reference, string dataObject)
    {
        var model = new IedModel("TEST");
        var ld = new LogicalDevice("LD", model);
        var lphd = new LogicalNode("LPHD1", ld);
        var doNode = new DataObject(dataObject, lphd);
        var da = new DataAttribute("stVal", doNode, DataAttributeType.BOOLEAN, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        registry.Register(new DevicePoint
        {
            Reference = reference,
            EquipmentName = "",
            LogicalDevice = "LD",
            LogicalNode = "LPHD1",
            DataObject = dataObject,
            LnType = eLnType.LPHD,
            Fc = FunctionalConstraint.ST,
            ValueAttribute = da
        });

        return (registry, da);
    }
}