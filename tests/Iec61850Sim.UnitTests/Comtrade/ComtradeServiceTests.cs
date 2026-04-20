using IEC61850.Common;

using Iec61850Sim.Core.Comtrade;
using Iec61850Sim.Core.Comtrade.Models;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Iec61850Sim.UnitTests.TestHelpers;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace Iec61850Sim.UnitTests.Comtrade;

public class ComtradeServiceTests : IDisposable
{
    private readonly IPointRegistry _registry = Substitute.For<IPointRegistry>();
    private readonly ILogger<ComtradeService> _logger = Substitute.For<ILogger<ComtradeService>>();
    private readonly string _tempDir;

    public ComtradeServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ComtradeTests_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── GenerateFaultAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateFaultAsync_WithMmxuAndSwitchingPoints_ReturnsRecordWithCorrectChannelCounts()
    {
        var analogPoints = BuildAnalogPoints();
        var digitalPoints = BuildDigitalPoints();

        _registry.GetByFc(FunctionalConstraint.MX).Returns(analogPoints);
        _registry.GetByFc(FunctionalConstraint.ST).Returns(digitalPoints);

        var sut = CreateSut();

        var result = await sut.GenerateFaultAsync(new ComtradeOptions());

        Assert.Equal(analogPoints.Count, result.AnalogChannelCount);
        Assert.Equal(digitalPoints.Count, result.DigitalChannelCount);
    }

    [Fact]
    public async Task GenerateFaultAsync_WithGenerateZipTrue_CreatesZipFileOnDisk()
    {
        SetupEmptyRegistry();
        var sut = CreateSut();

        var result = await sut.GenerateFaultAsync(new ComtradeOptions { GenerateZip = true });

        Assert.True(File.Exists(result.FilePath), $"Arquivo esperado não encontrado: {result.FilePath}");
        Assert.EndsWith(".zip", result.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateFaultAsync_WithGenerateZipFalse_CreatesSeparateFilesOnDisk()
    {
        SetupEmptyRegistry();
        var sut = CreateSut();

        var result = await sut.GenerateFaultAsync(new ComtradeOptions { GenerateZip = false });

        // FilePath aponta para o .hdr; os outros dois devem existir com o mesmo nome base
        var basePath = Path.ChangeExtension(result.FilePath, null);
        Assert.True(File.Exists(basePath + ".hdr"), "Arquivo .hdr não encontrado");
        Assert.True(File.Exists(basePath + ".cfg"), "Arquivo .cfg não encontrado");
        Assert.True(File.Exists(basePath + ".dat"), "Arquivo .dat não encontrado");
    }

    [Fact]
    public async Task GenerateFaultAsync_WithPoints_RecordNameFollowsFaultPattern()
    {
        SetupEmptyRegistry();
        var sut = CreateSut();

        var result = await sut.GenerateFaultAsync(new ComtradeOptions());

        Assert.StartsWith("Fault_", result.RecordName);
    }

    [Fact]
    public async Task GenerateFaultAsync_WithPoints_CfgInsideZipContainsStationName()
    {
        SetupEmptyRegistry();
        var sut = CreateSut();

        var result = await sut.GenerateFaultAsync(new ComtradeOptions { GenerateZip = true });

        using var zip = System.IO.Compression.ZipFile.OpenRead(result.FilePath);
        var cfgEntry = zip.Entries.Single(e => e.Name.EndsWith(".cfg"));
        using var reader = new StreamReader(cfgEntry.Open());
        var cfgContent = await reader.ReadToEndAsync();

        Assert.Contains("Demo", cfgContent);
        Assert.Contains("1999", cfgContent);
        Assert.Contains("ASCII", cfgContent);
    }

    // ── GetGeneratedRecords ────────────────────────────────────────────────

    [Fact]
    public async Task GetGeneratedRecords_AfterTwoGenerations_ReturnsBothRecords()
    {
        SetupEmptyRegistry();
        var sut = CreateSut();

        await sut.GenerateFaultAsync(new ComtradeOptions());
        await sut.GenerateFaultAsync(new ComtradeOptions());

        var records = sut.GetGeneratedRecords();

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public void GetGeneratedRecords_WithNoGenerations_ReturnsEmptyList()
    {
        var sut = CreateSut();

        var records = sut.GetGeneratedRecords();

        Assert.Empty(records);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ComtradeService CreateSut() =>
        new(_registry, _logger, _tempDir);

    private void SetupEmptyRegistry()
    {
        _registry.GetByFc(FunctionalConstraint.MX).Returns(Enumerable.Empty<DevicePoint>());
        _registry.GetByFc(FunctionalConstraint.ST).Returns(Enumerable.Empty<DevicePoint>());
    }

    private static List<DevicePoint> BuildAnalogPoints()
    {
        var current = DevicePointFactory.CreatePoint(
            "LD0/MMXU1.A.phsA.cVal.mag.f",
            "MMXU1", "A", eLnType.MMXU, FunctionalConstraint.MX);
        current.Value = 100.5f;

        var voltage = DevicePointFactory.CreatePoint(
            "LD0/MMXU1.PhV.phsA.cVal.mag.f",
            "MMXU1", "PhV", eLnType.MMXU, FunctionalConstraint.MX);
        voltage.Value = 127000.0f;

        return [current, voltage];
    }

    private static List<DevicePoint> BuildDigitalPoints()
    {
        var breaker = DevicePointFactory.CreatePoint(
            "LD0/XCBR1.Pos.stVal",
            "XCBR1", "Pos", eLnType.XCBR, FunctionalConstraint.ST);
        breaker.Value = (int)eDblPos.On;

        return [breaker];
    }
}