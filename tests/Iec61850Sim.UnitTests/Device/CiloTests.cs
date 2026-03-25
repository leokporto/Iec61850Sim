using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Iec61850Sim.UnitTests.TestHelpers;
using IEC61850.Common;
using Xunit;

namespace Iec61850Sim.UnitTests.Device;

public class CiloTests
{
    private const string EnaOpnRef = "LD0/CILO1.EnaOpn.stVal";
    private const string EnaClsRef = "LD0/CILO1.EnaCls.stVal";

    [Fact]
    public void CanOpen_WhenEnaOpnReferenceIsAbsent_ShouldReturnTrue()
    {
        var registry = new PointRegistry();
        var sut = new CILO("CILO1", "Q01");

        Assert.True(sut.CanOpen(registry));
    }

    [Fact]
    public void CanClose_WhenEnaClsReferenceIsAbsent_ShouldReturnTrue()
    {
        var registry = new PointRegistry();
        var sut = new CILO("CILO1", "Q01");

        Assert.True(sut.CanClose(registry));
    }

    [Fact]
    public void CanOpen_WhenEnaOpnIsTrue_ShouldReturnTrue()
    {
        var (registry, sut) = CreateCiloWithPoints();
        registry.SetValue(new PointValue<object> { Reference = EnaOpnRef, Value = true, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        Assert.True(sut.CanOpen(registry));
    }

    [Fact]
    public void CanOpen_WhenEnaOpnIsFalse_ShouldReturnFalse()
    {
        var (registry, sut) = CreateCiloWithPoints();
        registry.SetValue(new PointValue<object> { Reference = EnaOpnRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        Assert.False(sut.CanOpen(registry));
    }

    [Fact]
    public void CanClose_WhenEnaClsIsTrue_ShouldReturnTrue()
    {
        var (registry, sut) = CreateCiloWithPoints();
        registry.SetValue(new PointValue<object> { Reference = EnaClsRef, Value = true, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        Assert.True(sut.CanClose(registry));
    }

    [Fact]
    public void CanClose_WhenEnaClsIsFalse_ShouldReturnFalse()
    {
        var (registry, sut) = CreateCiloWithPoints();
        registry.SetValue(new PointValue<object> { Reference = EnaClsRef, Value = false, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        Assert.False(sut.CanClose(registry));
    }

    private static (PointRegistry registry, CILO cilo) CreateCiloWithPoints()
    {
        var registry = new PointRegistry();
        registry.Register(DevicePointFactory.CreatePoint(EnaOpnRef, "CILO1", "EnaOpn", eLnType.CILO, FunctionalConstraint.ST));
        registry.Register(DevicePointFactory.CreatePoint(EnaClsRef, "CILO1", "EnaCls", eLnType.CILO, FunctionalConstraint.ST));

        registry.SetValue(new PointValue<object> { Reference = EnaOpnRef, Value = true, Quality = 192, Timestamp = DateTimeOffset.UtcNow });
        registry.SetValue(new PointValue<object> { Reference = EnaClsRef, Value = true, Quality = 192, Timestamp = DateTimeOffset.UtcNow });

        var cilo = new CILO("CILO1", "Q01", enaOpnRef: EnaOpnRef, enaClsRef: EnaClsRef);
        return (registry, cilo);
    }
}
