using Iec61850Sim.Core.Comtrade.Models;

namespace Iec61850Sim.Core.Comtrade;

internal enum SegmentState
{
    /// <summary>Carga normal, disjuntor fechado.</summary>
    Normal,

    /// <summary>Falta presente, disjuntor fechado.</summary>
    Fault,

    /// <summary>Disjuntor aberto: correntes nulas.</summary>
    Open
}

/// <param name="StartSample">Índice (base 0) da primeira amostra do segmento.</param>
/// <param name="Length">Número de amostras previstas para o segmento.</param>
/// <param name="DigitalToggled">Canais digitais ficam no estado oposto ao normal durante o segmento.</param>
internal readonly record struct TimelineSegment(int StartSample, int Length, SegmentState State, bool DigitalToggled);

/// <summary>
/// Sequência de estados do registro, em amostras. O último segmento (pós-falta) pode ser
/// estendido indefinidamente quando o tamanho do arquivo é definido por um alvo em bytes.
/// </summary>
internal sealed class FaultTimeline
{
    private readonly TimelineSegment[] _segments;

    private FaultTimeline(TimelineSegment[] segments)
    {
        _segments = segments;
    }

    internal IReadOnlyList<TimelineSegment> Segments => _segments;

    /// <summary>Amostras de todos os segmentos exceto o último; sempre presentes no registro.</summary>
    internal int FixedSampleCount => _segments[^1].StartSample;

    /// <summary>Total de amostras quando o pós-falta tem a duração configurada.</summary>
    internal int NominalSampleCount => _segments[^1].StartSample + _segments[^1].Length;

    internal static FaultTimeline Build(ComtradeWaveformOptions options, int sampleRateHz)
    {
        int pre = MsToSamples(options.PreFaultMs, sampleRateHz);
        int fault = Math.Max(1, MsToSamples(options.FaultMs, sampleRateHz));
        int post = MsToSamples(options.PostFaultMs, sampleRateHz);

        var layout = options.Scenario == ComtradeScenario.AutoReclose
            ? new (int Length, SegmentState State, bool Toggled)[]
            {
                (pre, SegmentState.Normal, false),
                (fault, SegmentState.Fault, false),
                (MsToSamples(options.DeadTimeMs, sampleRateHz), SegmentState.Open, true),
                (fault, SegmentState.Fault, false),
                (post, SegmentState.Open, true)
            }
            : new (int Length, SegmentState State, bool Toggled)[]
            {
                (pre, SegmentState.Normal, false),
                (fault, SegmentState.Fault, true),
                (post, SegmentState.Normal, false)
            };

        var segments = new TimelineSegment[layout.Length];
        int start = 0;
        for (int i = 0; i < layout.Length; i++)
        {
            segments[i] = new TimelineSegment(start, layout[i].Length, layout[i].State, layout[i].Toggled);
            start += layout[i].Length;
        }

        return new FaultTimeline(segments);
    }

    /// <summary>Segmento que contém a amostra; além do fim previsto, retorna o último segmento.</summary>
    internal TimelineSegment SegmentAt(int sample)
    {
        for (int i = _segments.Length - 1; i > 0; i--)
        {
            if (sample >= _segments[i].StartSample)
                return _segments[i];
        }

        return _segments[0];
    }

    internal static int MsToSamples(int ms, int sampleRateHz) =>
        (int)Math.Round(ms * (double)sampleRateHz / 1000.0);
}
