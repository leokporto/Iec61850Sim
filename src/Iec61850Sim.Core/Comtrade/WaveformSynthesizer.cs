using Iec61850Sim.Core.Comtrade.Models;

namespace Iec61850Sim.Core.Comtrade;

internal enum ChannelKind
{
    /// <summary>Corrente instantânea (senoide).</summary>
    Current,

    /// <summary>Tensão instantânea (senoide).</summary>
    Voltage,

    /// <summary>Grandeza não senoidal (potência, frequência, ângulo...): segue o valor RMS.</summary>
    Trend
}

/// <param name="Index">Posição do canal; diferencia as sequências de ruído.</param>
/// <param name="NominalValue">Valor RMS (ou valor da tendência) em regime normal.</param>
/// <param name="IsFaulted">A fase do canal participa da falta do cenário.</param>
internal readonly record struct ChannelSpec(
    int Index,
    ChannelKind Kind,
    double NominalValue,
    double PhaseAngleDeg,
    bool IsFaulted,
    double FaultMultiplier);

/// <summary>
/// Sintetiza amostras instantâneas de forma determinística: o valor de cada amostra depende
/// apenas do canal, da linha do tempo, do índice e da semente (acesso aleatório, sem estado).
/// </summary>
internal static class WaveformSynthesizer
{
    internal const double LINE_FREQUENCY = 60.0;
    internal const double FAULT_CURRENT_MULTIPLIER = 3.0;
    internal const double FAULT_VOLTAGE_MULTIPLIER = 0.6;

    private const double DC_TIME_CONSTANT_S = 0.030;
    private const double THIRD_HARMONIC = 0.04;
    private const double FIFTH_HARMONIC = 0.02;
    private const double NOISE_RATIO = 0.005;
    private static readonly double Sqrt2 = Math.Sqrt(2.0);

    internal static ChannelSpec CreateSpec(
        int index, string reference, string dataObject, double nominalValue, ComtradeScenario scenario)
    {
        var lower = reference.ToLowerInvariant();
        bool isMagnitude = lower.Contains(".mag.");

        var kind = (dataObject, isMagnitude) switch
        {
            ("A", true) => ChannelKind.Current,
            ("PhV" or "PPV", true) => ChannelKind.Voltage,
            _ => ChannelKind.Trend
        };

        var phases = PhasesOf(lower);
        var faultedPhases = FaultedPhases(scenario);
        bool isFaulted = kind switch
        {
            ChannelKind.Current => phases.Overlaps(faultedPhases),
            // Tensão de neutro não afunda; considera só as fases A/B/C.
            ChannelKind.Voltage => phases.Overlaps(faultedPhases.Where(p => p != 'N')),
            _ => false
        };

        double multiplier = kind switch
        {
            ChannelKind.Current => FAULT_CURRENT_MULTIPLIER,
            ChannelKind.Voltage => FAULT_VOLTAGE_MULTIPLIER,
            _ => 1.0
        };

        return new ChannelSpec(index, kind, nominalValue, PhaseAngleOf(lower), isFaulted, multiplier);
    }

    internal static double ValueAt(
        ChannelSpec channel, FaultTimeline timeline, int sampleRateHz, int sample,
        bool harmonicsAndNoise, ulong seed)
    {
        var segment = timeline.SegmentAt(sample);
        double noise = harmonicsAndNoise ? NOISE_RATIO * Noise(seed, channel.Index, sample) : 0.0;

        if (channel.Kind == ChannelKind.Trend)
            return channel.NominalValue * (1.0 + noise);

        double rms = segment.State switch
        {
            SegmentState.Fault when channel.IsFaulted => channel.NominalValue * channel.FaultMultiplier,
            SegmentState.Open when channel.Kind == ChannelKind.Current => 0.0,
            _ => channel.NominalValue
        };

        double t = sample / (double)sampleRateHz;
        double omega = 2.0 * Math.PI * LINE_FREQUENCY;
        double angle = omega * t + channel.PhaseAngleDeg * Math.PI / 180.0;
        double peak = Sqrt2 * rms;

        double value = peak * Math.Sin(angle);

        if (harmonicsAndNoise)
            value += peak * (THIRD_HARMONIC * Math.Sin(3 * angle) + FIFTH_HARMONIC * Math.Sin(5 * angle));

        // Componente DC exponencial no início de cada falta: a corrente parte de zero.
        if (segment.State == SegmentState.Fault && channel.IsFaulted && channel.Kind == ChannelKind.Current)
        {
            double t0 = segment.StartSample / (double)sampleRateHz;
            double angle0 = omega * t0 + channel.PhaseAngleDeg * Math.PI / 180.0;
            value -= peak * Math.Sin(angle0) * Math.Exp(-(t - t0) / DC_TIME_CONSTANT_S);
        }

        // Ruído relativo ao valor nominal: canais abertos não ficam perfeitamente planos.
        return value + Sqrt2 * channel.NominalValue * noise;
    }

    /// <summary>Limite superior de |valor| do canal, usado para o fator de escala do .cfg.</summary>
    internal static double PeakBound(ChannelSpec channel, bool harmonicsAndNoise)
    {
        double nominal = Math.Abs(channel.NominalValue);
        double noise = harmonicsAndNoise ? NOISE_RATIO : 0.0;

        if (channel.Kind == ChannelKind.Trend)
            return nominal * (1.0 + noise);

        // Fases sãs não mudam na falta: limite menor preserva a resolução do int16.
        double harmonics = harmonicsAndNoise ? THIRD_HARMONIC + FIFTH_HARMONIC : 0.0;
        double multiplier = channel.IsFaulted ? Math.Max(1.0, channel.FaultMultiplier) : 1.0;
        double peak = Sqrt2 * nominal * multiplier;
        double dc = channel.IsFaulted && channel.Kind == ChannelKind.Current ? 1.0 : 0.0;

        return peak * (1.0 + harmonics + dc) + Sqrt2 * nominal * noise;
    }

    /// <summary>Semente estável (FNV-1a), independente do processo — ao contrário de string.GetHashCode.</summary>
    internal static ulong SeedFrom(string text)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash;
    }

    // Ruído uniforme em [-1, 1) derivado de (semente, canal, amostra) via splitmix64.
    private static double Noise(ulong seed, int channel, int sample)
    {
        ulong x = seed ^ ((ulong)(uint)channel * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)sample * 0xBF58476D1CE4E5B9UL);
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;

        return (x >> 11) * (1.0 / (1UL << 53)) * 2.0 - 1.0;
    }

    private static HashSet<char> PhasesOf(string lowerReference)
    {
        if (lowerReference.Contains(".phsab")) return ['A', 'B'];
        if (lowerReference.Contains(".phsbc")) return ['B', 'C'];
        if (lowerReference.Contains(".phsca")) return ['C', 'A'];
        if (lowerReference.Contains(".phsa")) return ['A'];
        if (lowerReference.Contains(".phsb")) return ['B'];
        if (lowerReference.Contains(".phsc")) return ['C'];
        if (lowerReference.Contains(".neut") || lowerReference.Contains(".res")) return ['N'];
        return [];
    }

    private static double PhaseAngleOf(string lowerReference)
    {
        if (lowerReference.Contains(".phsab")) return 30.0;
        if (lowerReference.Contains(".phsbc")) return -90.0;
        if (lowerReference.Contains(".phsca")) return 150.0;
        if (lowerReference.Contains(".phsb")) return -120.0;
        if (lowerReference.Contains(".phsc")) return 120.0;
        return 0.0;
    }

    private static char[] FaultedPhases(ComtradeScenario scenario) => scenario switch
    {
        ComtradeScenario.PhaseToPhase => ['B', 'C'],
        ComtradeScenario.ThreePhase => ['A', 'B', 'C'],
        _ => ['A', 'N']
    };
}
