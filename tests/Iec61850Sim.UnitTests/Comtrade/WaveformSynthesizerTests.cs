using Iec61850Sim.Core.Comtrade;
using Iec61850Sim.Core.Comtrade.Models;

using Xunit;

namespace Iec61850Sim.UnitTests.Comtrade;

public class WaveformSynthesizerTests
{
    private const int SAMPLES_PER_CYCLE = 24;
    private const int SAMPLE_RATE = SAMPLES_PER_CYCLE * 60;
    private const double NOMINAL_CURRENT = 100.0;

    private static readonly ComtradeWaveformOptions Options = new()
    {
        SamplesPerCycle = SAMPLES_PER_CYCLE,
        PreFaultMs = 100,
        FaultMs = 200,
        PostFaultMs = 100,
        HarmonicsAndNoise = false
    };

    [Fact]
    public void ValueAt_PreFault_RmsMatchesNominal()
    {
        var channel = CurrentChannel("LD0/MMXU1.A.phsA.cVal.mag.f");
        var timeline = FaultTimeline.Build(Options, SAMPLE_RATE);

        double rms = CycleRms(channel, timeline, startSample: 0);

        Assert.Equal(NOMINAL_CURRENT, rms, tolerance: 0.5);
    }

    [Fact]
    public void ValueAt_PhaseB_LagsPhaseA120Degrees()
    {
        var phaseA = CurrentChannel("LD0/MMXU1.A.phsA.cVal.mag.f");
        var phaseB = CurrentChannel("LD0/MMXU1.A.phsB.cVal.mag.f");
        var timeline = FaultTimeline.Build(Options, SAMPLE_RATE);
        const int lag = SAMPLES_PER_CYCLE / 3;

        for (int s = lag; s < SAMPLES_PER_CYCLE * 2; s++)
        {
            double a = WaveformSynthesizer.ValueAt(phaseA, timeline, SAMPLE_RATE, s - lag, false, 0);
            double b = WaveformSynthesizer.ValueAt(phaseB, timeline, SAMPLE_RATE, s, false, 0);
            Assert.Equal(a, b, tolerance: 1e-6);
        }
    }

    [Fact]
    public void ValueAt_FaultWindowAfterDcDecay_FaultedCurrentRmsMultiplied()
    {
        var channel = CurrentChannel("LD0/MMXU1.A.phsA.cVal.mag.f");
        var timeline = FaultTimeline.Build(Options, SAMPLE_RATE);
        var fault = timeline.Segments[1];

        // Último ciclo da falta: componente DC (τ = 30 ms) já desprezível após ~180 ms.
        double rms = CycleRms(channel, timeline, fault.StartSample + fault.Length - SAMPLES_PER_CYCLE);

        Assert.Equal(NOMINAL_CURRENT * WaveformSynthesizer.FAULT_CURRENT_MULTIPLIER, rms, tolerance: 1.0);
    }

    [Fact]
    public void ValueAt_SinglePhaseToGround_HealthyPhaseKeepsNominal()
    {
        var channel = CurrentChannel("LD0/MMXU1.A.phsB.cVal.mag.f");
        var timeline = FaultTimeline.Build(Options, SAMPLE_RATE);
        var fault = timeline.Segments[1];

        double rms = CycleRms(channel, timeline, fault.StartSample + SAMPLES_PER_CYCLE);

        Assert.False(channel.IsFaulted);
        Assert.Equal(NOMINAL_CURRENT, rms, tolerance: 0.5);
    }

    [Fact]
    public void ValueAt_AutoReclose_HasTwoFaultIntervalsAndZeroCurrentWhenOpen()
    {
        var options = Options with { Scenario = ComtradeScenario.AutoReclose };
        var channel = CurrentChannel("LD0/MMXU1.A.phsA.cVal.mag.f", ComtradeScenario.AutoReclose);
        var timeline = FaultTimeline.Build(options, SAMPLE_RATE);

        var states = timeline.Segments.Select(s => s.State).ToArray();
        Assert.Equal(
            [SegmentState.Normal, SegmentState.Fault, SegmentState.Open, SegmentState.Fault, SegmentState.Open],
            states);

        var deadTime = timeline.Segments[2];
        Assert.Equal(0.0, CycleRms(channel, timeline, deadTime.StartSample), tolerance: 1e-9);

        var reclose = timeline.Segments[3];
        double recloseRms = CycleRms(channel, timeline, reclose.StartSample + reclose.Length - SAMPLES_PER_CYCLE);
        Assert.Equal(NOMINAL_CURRENT * WaveformSynthesizer.FAULT_CURRENT_MULTIPLIER, recloseRms, tolerance: 1.0);
    }

    [Fact]
    public void ValueAt_SameSeed_IsDeterministic()
    {
        var channel = CurrentChannel("LD0/MMXU1.A.phsA.cVal.mag.f");
        var timeline = FaultTimeline.Build(Options, SAMPLE_RATE);
        ulong seed = WaveformSynthesizer.SeedFrom("Fault_20260925_120000000");

        var first = Enumerable.Range(0, 200)
            .Select(s => WaveformSynthesizer.ValueAt(channel, timeline, SAMPLE_RATE, s, true, seed)).ToArray();
        var second = Enumerable.Range(0, 200)
            .Select(s => WaveformSynthesizer.ValueAt(channel, timeline, SAMPLE_RATE, s, true, seed)).ToArray();

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("LD0/MMXU1.A.phsA.cVal.mag.f", "A")]
    [InlineData("LD0/MMXU1.A.phsB.cVal.mag.f", "A")]
    [InlineData("LD0/MMXU1.PhV.phsA.cVal.mag.f", "PhV")]
    [InlineData("LD0/MMXU1.PhV.phsC.cVal.mag.f", "PhV")]
    public void ValueAt_WithHarmonicsAndNoise_NeverExceedsPeakBound(string reference, string dataObject)
    {
        var options = Options with { HarmonicsAndNoise = true, Scenario = ComtradeScenario.AutoReclose };
        var channel = WaveformSynthesizer.CreateSpec(0, reference, dataObject, NOMINAL_CURRENT, ComtradeScenario.AutoReclose);
        var timeline = FaultTimeline.Build(options, SAMPLE_RATE);
        double bound = WaveformSynthesizer.PeakBound(channel, harmonicsAndNoise: true);

        for (int s = 0; s < timeline.NominalSampleCount; s++)
        {
            double value = WaveformSynthesizer.ValueAt(channel, timeline, SAMPLE_RATE, s, true, 42);
            Assert.True(Math.Abs(value) <= bound, $"Amostra {s}: |{value}| > {bound}");
        }
    }

    [Theory]
    [InlineData("LD0/MMXU1.A.phsA.cVal.ang.f", "A")]
    [InlineData("LD0/MMXU1.TotW.mag.f", "TotW")]
    [InlineData("LD0/MMXU1.Hz.mag.f", "Hz")]
    public void CreateSpec_NonSinusoidalQuantity_IsTrend(string reference, string dataObject)
    {
        var spec = WaveformSynthesizer.CreateSpec(0, reference, dataObject, 10.0, ComtradeScenario.ThreePhase);

        Assert.Equal(ChannelKind.Trend, spec.Kind);
    }

    [Fact]
    public void CreateSpec_PhaseToPhase_FaultsOnlyPhasesBAndC()
    {
        var a = WaveformSynthesizer.CreateSpec(0, "LD0/MMXU1.A.phsA.cVal.mag.f", "A", 1, ComtradeScenario.PhaseToPhase);
        var b = WaveformSynthesizer.CreateSpec(1, "LD0/MMXU1.A.phsB.cVal.mag.f", "A", 1, ComtradeScenario.PhaseToPhase);
        var c = WaveformSynthesizer.CreateSpec(2, "LD0/MMXU1.A.phsC.cVal.mag.f", "A", 1, ComtradeScenario.PhaseToPhase);

        Assert.False(a.IsFaulted);
        Assert.True(b.IsFaulted);
        Assert.True(c.IsFaulted);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ChannelSpec CurrentChannel(
        string reference, ComtradeScenario scenario = ComtradeScenario.SinglePhaseToGround) =>
        WaveformSynthesizer.CreateSpec(0, reference, "A", NOMINAL_CURRENT, scenario);

    private static double CycleRms(ChannelSpec channel, FaultTimeline timeline, int startSample)
    {
        double sumSquares = 0;
        for (int s = startSample; s < startSample + SAMPLES_PER_CYCLE; s++)
        {
            double v = WaveformSynthesizer.ValueAt(channel, timeline, SAMPLE_RATE, s, false, 0);
            sumSquares += v * v;
        }

        return Math.Sqrt(sumSquares / SAMPLES_PER_CYCLE);
    }
}
