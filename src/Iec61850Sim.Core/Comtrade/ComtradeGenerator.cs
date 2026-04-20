using System.Globalization;
using System.Text;

using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Comtrade;

/// <summary>
/// Gera o conteúdo dos arquivos COMTRADE 1999 (.hdr, .cfg, .dat) em memória.
/// Formato ASCII, 60 Hz (padrão brasileiro), 1000 amostras/segundo.
/// Janela de falta: 100 ms pré-falta → 200 ms falta → 100 ms pós-falta.
/// </summary>
internal static class ComtradeGenerator
{
    private const double FAULT_CURRENT_MULTIPLIER = 3.0;
    private const double FAULT_VOLTAGE_MULTIPLIER = 0.6;
    private const int SAMPLE_RATE_HZ = 1000;
    private const int PRE_FAULT_SAMPLES = 100;
    private const int FAULT_SAMPLES = 200;
    private const int POST_FAULT_SAMPLES = 100;
    private const int TOTAL_SAMPLES = PRE_FAULT_SAMPLES + FAULT_SAMPLES + POST_FAULT_SAMPLES;
    private const double LINE_FREQUENCY = 60.0;
    private const string STATION_NAME = "Demo";
    private const string CRLF = "\r\n";

    internal record GeneratedContent(string Hdr, string Cfg, string Dat);

    internal static GeneratedContent Generate(
        string recordName,
        DateTime timestamp,
        IReadOnlyList<DevicePoint> analogPoints,
        IReadOnlyList<DevicePoint> digitalPoints)
    {
        // Computa amostras analógicas e digitais para todos os instantes de tempo
        var analogSamples = ComputeAnalogSamples(analogPoints);
        var digitalSamples = ComputeDigitalSamples(digitalPoints);

        // Determina fator de escala por canal analógico (a, b, min, max inteiros)
        var scaleFactors = ComputeScaleFactors(analogSamples, analogPoints.Count);

        var hdr = BuildHdr(recordName, timestamp);
        var cfg = BuildCfg(recordName, timestamp, analogPoints, digitalPoints, scaleFactors);
        var dat = BuildDat(analogSamples, digitalSamples, scaleFactors, analogPoints.Count, digitalPoints.Count);

        return new GeneratedContent(hdr, cfg, dat);
    }

    // ── Geração de amostras ────────────────────────────────────────────────

    private static double[][] ComputeAnalogSamples(IReadOnlyList<DevicePoint> points)
    {
        var result = new double[points.Count][];

        for (int ch = 0; ch < points.Count; ch++)
        {
            var point = points[ch];
            double nominal = ToDouble(point.Value);
            double faultMultiplier = GetFaultMultiplier(point.DataObject);

            result[ch] = new double[TOTAL_SAMPLES];
            for (int s = 0; s < TOTAL_SAMPLES; s++)
            {
                bool inFault = s >= PRE_FAULT_SAMPLES && s < PRE_FAULT_SAMPLES + FAULT_SAMPLES;
                result[ch][s] = inFault ? nominal * faultMultiplier : nominal;
            }
        }

        return result;
    }

    private static int[][] ComputeDigitalSamples(IReadOnlyList<DevicePoint> points)
    {
        var result = new int[points.Count][];

        for (int ch = 0; ch < points.Count; ch++)
        {
            // eDblPos: Off=1 → COMTRADE 0, On=2 → COMTRADE 1
            int nominalState = ToDigitalState(points[ch].Value);
            int faultState = nominalState == 0 ? 1 : 0;

            result[ch] = new int[TOTAL_SAMPLES];
            for (int s = 0; s < TOTAL_SAMPLES; s++)
            {
                bool inFault = s >= PRE_FAULT_SAMPLES && s < PRE_FAULT_SAMPLES + FAULT_SAMPLES;
                result[ch][s] = inFault ? faultState : nominalState;
            }
        }

        return result;
    }

    // ── Fator de escala analógico ──────────────────────────────────────────

    private static (double a, double b, int minInt, int maxInt)[] ComputeScaleFactors(
        double[][] samples, int channelCount)
    {
        var result = new (double a, double b, int minInt, int maxInt)[channelCount];

        for (int ch = 0; ch < channelCount; ch++)
        {
            double maxAbs = samples[ch].Length > 0 ? samples[ch].Max(v => Math.Abs(v)) : 0;

            // a = fator de conversão: valor_real = a * valor_armazenado + b
            double a = maxAbs > 0 ? maxAbs / 32767.0 : 1.0;
            const double b = 0.0;

            int minInt = samples[ch].Length > 0
                ? samples[ch].Min(v => (int)Math.Round(v / a))
                : 0;
            int maxInt = samples[ch].Length > 0
                ? samples[ch].Max(v => (int)Math.Round(v / a))
                : 0;

            result[ch] = (a, b, minInt, maxInt);
        }

        return result;
    }

    // ── Construção dos arquivos ────────────────────────────────────────────

    private static string BuildHdr(string recordName, DateTime timestamp)
    {
        var sb = new StringBuilder();
        sb.Append($"IEC 61850 Simulator - Exportação COMTRADE{CRLF}");
        sb.Append($"Registro: {recordName}{CRLF}");
        sb.Append($"Gerado em: {timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}{CRLF}");
        sb.Append($"Estação: {STATION_NAME}{CRLF}");
        sb.Append($"Janela: {PRE_FAULT_SAMPLES} ms pré-falta, {FAULT_SAMPLES} ms falta, {POST_FAULT_SAMPLES} ms pós-falta{CRLF}");
        sb.Append($"Taxa de amostragem: {SAMPLE_RATE_HZ} Sa/s{CRLF}");
        sb.Append($"Frequência nominal: {LINE_FREQUENCY} Hz{CRLF}");
        return sb.ToString();
    }

    private static string BuildCfg(
        string recordName,
        DateTime timestamp,
        IReadOnlyList<DevicePoint> analogPoints,
        IReadOnlyList<DevicePoint> digitalPoints,
        (double a, double b, int minInt, int maxInt)[] scaleFactors)
    {
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;

        int analogCount = analogPoints.Count;
        int digitalCount = digitalPoints.Count;
        int totalChannels = analogCount + digitalCount;

        // Linha 1: identificação
        sb.Append($"{STATION_NAME},{recordName},1999{CRLF}");

        // Linha 2: total de canais
        sb.Append($"{totalChannels},{analogCount}A,{digitalCount}D{CRLF}");

        // Definição dos canais analógicos
        // Formato: an,ch_id,ph,ccbm,uu,a,b,skew,min,max,primary,secondary,PS
        for (int i = 0; i < analogCount; i++)
        {
            var p = analogPoints[i];
            var (a, b, minInt, maxInt) = scaleFactors[i];

            string chId = ExtractChannelId(p.Reference);
            string phase = ExtractPhase(p.Reference);
            string unit = GetUnit(p.DataObject);

            sb.Append(string.Format(ci,
                "{0},{1},{2},,{3},{4:G8},{5:G8},0,{6},{7},1,1,P{8}",
                i + 1, chId, phase, unit, a, b, minInt, maxInt, CRLF));
        }

        // Definição dos canais digitais
        // Formato: dn,ch_id,ph,ccbm,y
        for (int i = 0; i < digitalCount; i++)
        {
            var p = digitalPoints[i];
            string chId = ExtractChannelId(p.Reference);
            int normalState = ToDigitalState(p.Value);

            sb.Append(string.Format(ci, "{0},{1},,,{2}{3}", i + 1, chId, normalState, CRLF));
        }

        // Frequência nominal da linha (Hz)
        sb.Append(string.Format(ci, "{0}{1}", LINE_FREQUENCY, CRLF));

        // Número de taxas de amostragem
        sb.Append($"1{CRLF}");

        // Taxa de amostragem e amostra final
        sb.Append(string.Format(ci, "{0},{1}{2}", SAMPLE_RATE_HZ, TOTAL_SAMPLES, CRLF));

        // Instante de início do registro
        sb.Append($"{timestamp.ToString("dd/MM/yyyy,HH:mm:ss.ffffff", ci)}{CRLF}");

        // Instante de disparo (início da falta = PRE_FAULT_SAMPLES ms após início)
        var triggerTime = timestamp.AddMilliseconds(PRE_FAULT_SAMPLES);
        sb.Append($"{triggerTime.ToString("dd/MM/yyyy,HH:mm:ss.ffffff", ci)}{CRLF}");

        // Tipo do arquivo de dados
        sb.Append($"ASCII{CRLF}");

        // Multiplicador de tempo (1 = microsegundos)
        sb.Append($"1{CRLF}");

        return sb.ToString();
    }

    private static string BuildDat(
        double[][] analogSamples,
        int[][] digitalSamples,
        (double a, double b, int minInt, int maxInt)[] scaleFactors,
        int analogCount,
        int digitalCount)
    {
        var sb = new StringBuilder(capacity: TOTAL_SAMPLES * (analogCount + digitalCount + 2) * 8);
        var ci = CultureInfo.InvariantCulture;

        for (int s = 0; s < TOTAL_SAMPLES; s++)
        {
            // Timestamp em microsegundos a partir do início do registro
            long timestampMicros = (long)s * 1_000_000L / SAMPLE_RATE_HZ;

            sb.Append(string.Format(ci, "{0},{1}", s + 1, timestampMicros));

            // Valores analógicos: inteiros armazenados (valor_real = a * stored + b)
            for (int ch = 0; ch < analogCount; ch++)
            {
                var (a, _, _, _) = scaleFactors[ch];
                int stored = a > 0 ? (int)Math.Round(analogSamples[ch][s] / a) : 0;
                sb.Append(',');
                sb.Append(stored.ToString(ci));
            }

            // Valores digitais (separados por vírgula)
            for (int ch = 0; ch < digitalCount; ch++)
            {
                sb.Append(',');
                sb.Append(digitalSamples[ch][s].ToString(ci));
            }

            sb.Append(CRLF);
        }

        return sb.ToString();
    }

    // ── Auxiliares ────────────────────────────────────────────────────────

    private static double ToDouble(object? value) => value switch
    {
        float f => f,
        double d => d,
        int i => i,
        _ => 0.0
    };

    private static int ToDigitalState(object? value)
    {
        // eDblPos.On (2) → COMTRADE 1 (fechado); qualquer outro → 0 (aberto)
        if (value is int i) return i == (int)eDblPos.On ? 1 : 0;
        return 0;
    }

    private static double GetFaultMultiplier(string dataObject) => dataObject switch
    {
        "A" => FAULT_CURRENT_MULTIPLIER,
        "PhV" or "PPV" => FAULT_VOLTAGE_MULTIPLIER,
        _ => 1.0
    };

    private static string ExtractChannelId(string reference)
    {
        // Remove o prefixo do dispositivo lógico (ex: "LD0/MMXU1.A.mag.f" → "MMXU1.A.mag.f")
        int slash = reference.IndexOf('/');
        return slash >= 0 ? reference[(slash + 1)..] : reference;
    }

    private static string ExtractPhase(string reference)
    {
        var lower = reference.ToLowerInvariant();
        if (lower.Contains(".phsa")) return "A";
        if (lower.Contains(".phsb")) return "B";
        if (lower.Contains(".phsc")) return "C";
        if (lower.Contains(".neut")) return "N";
        return "";
    }

    private static string GetUnit(string dataObject) => dataObject switch
    {
        "A" => "A",
        "PhV" or "PPV" => "kV",
        "W" or "TotW" => "W",
        "VAr" or "TotVAr" => "VAr",
        "VA" or "TotVA" => "VA",
        "Hz" => "Hz",
        _ => ""
    };
}