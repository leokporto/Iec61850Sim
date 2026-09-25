using System.Buffers.Binary;
using System.Text;

using IEC61850.Common;

using Iec61850Sim.Core.Comtrade;
using Iec61850Sim.Core.Comtrade.Models;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.UnitTests.TestHelpers;

using Xunit;

namespace Iec61850Sim.UnitTests.Comtrade;

public class ComtradeGeneratorTests
{
    private const string RECORD_NAME = "Fault_20260925_120000000";
    private static readonly DateTime Timestamp = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Generate_Binary_DatLengthEqualsSamplesTimesRecordSize()
    {
        var analog = BuildAnalogPoints();
        var digital = BuildDigitalPoints(3);

        var content = Generate(analog, digital, new ComtradeWaveformOptions { DataFormat = ComtradeDataFormat.Binary });

        int recordSize = ComtradeGenerator.BinaryRecordSize(analog.Count, digital.Count);
        Assert.Equal(8 + 2 * analog.Count + 2, recordSize);
        Assert.Equal(content.SampleCount * recordSize, content.Dat.Length);
    }

    [Fact]
    public void Generate_Binary_CfgDeclaresBinary()
    {
        var content = Generate(BuildAnalogPoints(), BuildDigitalPoints(1),
            new ComtradeWaveformOptions { DataFormat = ComtradeDataFormat.Binary });

        var lines = CfgLines(content);
        Assert.Equal("BINARY", lines[^2]);
    }

    [Fact]
    public void Generate_SamplesPerCycle64_CfgSampleRate3840AndEndSample()
    {
        var content = Generate(BuildAnalogPoints(), BuildDigitalPoints(1),
            new ComtradeWaveformOptions { SamplesPerCycle = 64, PreFaultMs = 100, FaultMs = 200, PostFaultMs = 100 });

        var lines = CfgLines(content);
        Assert.Contains("3840,1536", lines);
        Assert.Equal(3840, content.SampleRateHz);
        Assert.Equal(1536, content.SampleCount);
    }

    [Fact]
    public void Generate_Ascii_EachSampleIsOneLineWithAllChannels()
    {
        var analog = BuildAnalogPoints();
        var digital = BuildDigitalPoints(2);

        var content = Generate(analog, digital, new ComtradeWaveformOptions());

        var lines = Encoding.ASCII.GetString(content.Dat)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(content.SampleCount, lines.Length);
        Assert.All(lines, l => Assert.Equal(2 + analog.Count + digital.Count, l.Split(',').Length));
    }

    [Fact]
    public void Generate_DigitalChannels_PackedIn16BitWords()
    {
        var analog = BuildAnalogPoints();
        var digital = BuildDigitalPoints(17); // 17 canais → 2 palavras

        var content = Generate(analog, digital, new ComtradeWaveformOptions { DataFormat = ComtradeDataFormat.Binary });

        int recordSize = ComtradeGenerator.BinaryRecordSize(analog.Count, digital.Count);
        Assert.Equal(8 + 2 * analog.Count + 4, recordSize);

        // Pré-falta: todos os disjuntores fechados (estado 1) → 16 bits na 1ª palavra, 1 bit na 2ª.
        var first = content.Dat.AsSpan(0, recordSize);
        int digitalOffset = 8 + 2 * analog.Count;
        Assert.Equal(0xFFFF, BinaryPrimitives.ReadUInt16LittleEndian(first[digitalOffset..]));
        Assert.Equal(0x0001, BinaryPrimitives.ReadUInt16LittleEndian(first[(digitalOffset + 2)..]));
    }

    [Theory]
    [InlineData(43008)]
    [InlineData(53248)]
    [InlineData(549120)]
    public void Generate_BinaryTargetSize_DatWithinOneRecordBelowTarget(long target)
    {
        var analog = BuildAnalogPoints();
        var digital = BuildDigitalPoints(1);

        var content = Generate(analog, digital, new ComtradeWaveformOptions
        {
            DataFormat = ComtradeDataFormat.Binary,
            TargetSizeBytes = target
        });

        int recordSize = ComtradeGenerator.BinaryRecordSize(analog.Count, digital.Count);
        Assert.InRange(content.Dat.Length, target - recordSize + 1, target);
    }

    [Theory]
    [InlineData(43008)]
    [InlineData(53248)]
    [InlineData(549120)]
    public void Generate_AsciiTargetSize_DatWithinOneLineBelowTarget(long target)
    {
        var content = Generate(BuildAnalogPoints(), BuildDigitalPoints(1),
            new ComtradeWaveformOptions { TargetSizeBytes = target });

        var dat = Encoding.ASCII.GetString(content.Dat);
        int longestLine = dat.Split("\r\n").Max(l => l.Length) + 2;
        Assert.InRange(content.Dat.Length, target - longestLine, target);
    }

    [Fact]
    public void Generate_TargetSmallerThanFaultWindow_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => Generate(BuildAnalogPoints(), BuildDigitalPoints(1),
            new ComtradeWaveformOptions { TargetSizeBytes = 100 }));

        Assert.Contains("mínimo", ex.Message);
    }

    [Fact]
    public void Generate_InvalidOptions_Throws()
    {
        Assert.Throws<ArgumentException>(() => Generate(BuildAnalogPoints(), BuildDigitalPoints(1),
            new ComtradeWaveformOptions { SamplesPerCycle = 0 }));
    }

    [Fact]
    public void Generate_AnalogStoredValues_VaryAcrossCycle()
    {
        // Regressão: antes da síntese de forma de onda, o canal era um valor constante.
        var content = Generate(BuildAnalogPoints(), [], new ComtradeWaveformOptions { SamplesPerCycle = 16 });

        var firstCycle = Encoding.ASCII.GetString(content.Dat)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Take(16)
            .Select(l => int.Parse(l.Split(',')[2]))
            .ToArray();

        Assert.True(firstCycle.Min() < 0 && firstCycle.Max() > 0, "Corrente deve oscilar em torno de zero");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ComtradeGenerator.GeneratedContent Generate(
        List<DevicePoint> analog, List<DevicePoint> digital, ComtradeWaveformOptions options) =>
        ComtradeGenerator.Generate(RECORD_NAME, Timestamp, analog, digital, options);

    private static string[] CfgLines(ComtradeGenerator.GeneratedContent content) =>
        content.Cfg.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    private static List<DevicePoint> BuildAnalogPoints()
    {
        var points = new List<DevicePoint>();
        foreach (var phase in new[] { "phsA", "phsB", "phsC" })
        {
            var current = DevicePointFactory.CreatePoint(
                $"LD0/MMXU1.A.{phase}.cVal.mag.f", "MMXU1", "A", eLnType.MMXU, FunctionalConstraint.MX);
            current.Value = 100.5f;
            points.Add(current);

            var voltage = DevicePointFactory.CreatePoint(
                $"LD0/MMXU1.PhV.{phase}.cVal.mag.f", "MMXU1", "PhV", eLnType.MMXU, FunctionalConstraint.MX);
            voltage.Value = 127000.0f;
            points.Add(voltage);
        }

        return points;
    }

    private static List<DevicePoint> BuildDigitalPoints(int count) =>
        Enumerable.Range(1, count).Select(i =>
        {
            var breaker = DevicePointFactory.CreatePoint(
                $"LD0/XCBR{i}.Pos.stVal", $"XCBR{i}", "Pos", eLnType.XCBR, FunctionalConstraint.ST);
            breaker.Value = (int)eDblPos.On;
            return breaker;
        }).ToList();
}
