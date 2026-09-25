using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using Iec61850Sim.Core.Comtrade.Models;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Comtrade;

/// <summary>
/// Gera o conteúdo dos arquivos COMTRADE 1999 (.hdr, .cfg, .dat) em memória.
/// Canais de corrente e tensão são senoides de 60 Hz (padrão brasileiro) sintetizadas pelo
/// <see cref="WaveformSynthesizer"/>; taxa, janela, cenário e formato vêm de
/// <see cref="ComtradeWaveformOptions"/>.
/// </summary>
internal static class ComtradeGenerator
{
    private const string STATION_NAME = "Demo";
    private const string CRLF = "\r\n";
    private const int MAX_STORED_VALUE = 32767;

    internal record GeneratedContent(string Hdr, string Cfg, byte[] Dat, int SampleCount, int SampleRateHz);

    internal static GeneratedContent Generate(
        string recordName,
        DateTime timestamp,
        IReadOnlyList<DevicePoint> analogPoints,
        IReadOnlyList<DevicePoint> digitalPoints,
        ComtradeWaveformOptions? options = null)
    {
        options ??= new ComtradeWaveformOptions();

        var errors = options.Validate();
        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors), nameof(options));

        int sampleRateHz = options.SamplesPerCycle * (int)WaveformSynthesizer.LINE_FREQUENCY;
        var timeline = FaultTimeline.Build(options, sampleRateHz);

        var channels = analogPoints
            .Select((p, i) => WaveformSynthesizer.CreateSpec(i, p.Reference, p.DataObject, ToDouble(p.Value), options.Scenario))
            .ToArray();

        // valor_real = a * valor_armazenado + b. O limite analítico garante que nenhuma
        // amostra ultrapasse ±32767, independentemente da quantidade de amostras.
        var scaleA = channels
            .Select(c => WaveformSynthesizer.PeakBound(c, options.HarmonicsAndNoise))
            .Select(bound => bound > 0 ? bound / MAX_STORED_VALUE : 1.0)
            .ToArray();

        var normalStates = digitalPoints.Select(p => ToDigitalState(p.Value)).ToArray();

        var dat = BuildDat(recordName, options, timeline, sampleRateHz, channels, scaleA, normalStates);

        var hdr = BuildHdr(recordName, timestamp, options, sampleRateHz, dat.SampleCount);
        var cfg = BuildCfg(recordName, timestamp, options, sampleRateHz, analogPoints, digitalPoints,
            scaleA, dat, normalStates);

        return new GeneratedContent(hdr, cfg, dat.Bytes, dat.SampleCount, sampleRateHz);
    }

    /// <summary>Tamanho de uma amostra no .dat binário: n e timestamp (uint32), int16 por canal analógico e palavras de 16 bits para os digitais.</summary>
    internal static int BinaryRecordSize(int analogCount, int digitalCount) =>
        8 + 2 * analogCount + 2 * DigitalWordCount(digitalCount);

    private static int DigitalWordCount(int digitalCount) => (digitalCount + 15) / 16;

    // ── Arquivo de dados ──────────────────────────────────────────────────

    private sealed record DatResult(byte[] Bytes, int SampleCount, int[] MinStored, int[] MaxStored);

    private static DatResult BuildDat(
        string recordName,
        ComtradeWaveformOptions options,
        FaultTimeline timeline,
        int sampleRateHz,
        ChannelSpec[] channels,
        double[] scaleA,
        int[] normalStates)
    {
        ulong seed = WaveformSynthesizer.SeedFrom(recordName);
        var min = Enumerable.Repeat(int.MaxValue, channels.Length).ToArray();
        var max = Enumerable.Repeat(int.MinValue, channels.Length).ToArray();
        var stored = new int[channels.Length];
        var digital = new int[normalStates.Length];

        using var stream = new MemoryStream();
        var line = new StringBuilder();
        var binary = new byte[BinaryRecordSize(channels.Length, normalStates.Length)];

        long? target = options.TargetSizeBytes;
        int sample = 0;

        while (true)
        {
            bool isFixed = sample < timeline.FixedSampleCount;
            if (!isFixed && target is null && sample >= timeline.NominalSampleCount)
                break;

            var segment = timeline.SegmentAt(sample);
            for (int ch = 0; ch < channels.Length; ch++)
            {
                double value = WaveformSynthesizer.ValueAt(
                    channels[ch], timeline, sampleRateHz, sample, options.HarmonicsAndNoise, seed);
                stored[ch] = Math.Clamp((int)Math.Round(value / scaleA[ch]), -MAX_STORED_VALUE, MAX_STORED_VALUE);
            }

            for (int ch = 0; ch < normalStates.Length; ch++)
                digital[ch] = segment.DigitalToggled ? 1 - normalStates[ch] : normalStates[ch];

            long timestampMicros = (long)Math.Round(sample * 1_000_000.0 / sampleRateHz);

            // O campo timestamp do .dat binário é uint32 em µs (~71 min).
            if (!isFixed && timestampMicros > uint.MaxValue)
                break;

            ReadOnlySpan<byte> bytes = options.DataFormat == ComtradeDataFormat.Binary
                ? EncodeBinary(binary, sample + 1, timestampMicros, stored, digital)
                : Encoding.ASCII.GetBytes(EncodeAscii(line, sample + 1, timestampMicros, stored, digital));

            // Pós-falta com alvo: para antes da amostra que ultrapassaria o tamanho desejado.
            if (!isFixed && target is not null && stream.Length + bytes.Length > target)
                break;

            stream.Write(bytes);
            for (int ch = 0; ch < channels.Length; ch++)
            {
                min[ch] = Math.Min(min[ch], stored[ch]);
                max[ch] = Math.Max(max[ch], stored[ch]);
            }

            sample++;
        }

        if (target is not null && stream.Length > target)
        {
            throw new ArgumentException(
                $"Tamanho alvo de {target} bytes é menor que o mínimo de {stream.Length} bytes " +
                "para a janela de pré-falta e falta configurada.", nameof(options));
        }

        return new DatResult(stream.ToArray(), sample, min, max);
    }

    private static string EncodeAscii(StringBuilder line, int sampleNumber, long timestampMicros, int[] stored, int[] digital)
    {
        var ci = CultureInfo.InvariantCulture;
        line.Clear();
        line.Append(sampleNumber.ToString(ci)).Append(',').Append(timestampMicros.ToString(ci));

        foreach (int value in stored)
            line.Append(',').Append(value.ToString(ci));

        foreach (int value in digital)
            line.Append(',').Append(value.ToString(ci));

        return line.Append(CRLF).ToString();
    }

    private static byte[] EncodeBinary(byte[] buffer, int sampleNumber, long timestampMicros, int[] stored, int[] digital)
    {
        var span = buffer.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)sampleNumber);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)timestampMicros);

        int offset = 8;
        foreach (int value in stored)
        {
            BinaryPrimitives.WriteInt16LittleEndian(span[offset..], (short)value);
            offset += 2;
        }

        // Canais digitais: bit 0 da primeira palavra = canal 1, e assim por diante.
        for (int word = 0; word < DigitalWordCount(digital.Length); word++)
        {
            ushort bits = 0;
            for (int bit = 0; bit < 16 && word * 16 + bit < digital.Length; bit++)
            {
                if (digital[word * 16 + bit] != 0)
                    bits |= (ushort)(1 << bit);
            }

            BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], bits);
            offset += 2;
        }

        return buffer;
    }

    // ── Cabeçalho e configuração ──────────────────────────────────────────

    private static string BuildHdr(
        string recordName, DateTime timestamp, ComtradeWaveformOptions options, int sampleRateHz, int sampleCount)
    {
        var sb = new StringBuilder();
        sb.Append($"IEC 61850 Simulator - Exportação COMTRADE{CRLF}");
        sb.Append($"Registro: {recordName}{CRLF}");
        sb.Append($"Gerado em: {timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}{CRLF}");
        sb.Append($"Estação: {STATION_NAME}{CRLF}");
        sb.Append($"Cenário: {options.Scenario}{CRLF}");

        if (options.Scenario == ComtradeScenario.AutoReclose)
            sb.Append($"Janela: {options.PreFaultMs} ms pré-falta, {options.FaultMs} ms falta, {options.DeadTimeMs} ms tempo morto, {options.FaultMs} ms religamento sob falta, abertura definitiva{CRLF}");
        else
            sb.Append($"Janela: {options.PreFaultMs} ms pré-falta, {options.FaultMs} ms falta, pós-falta até o fim do registro{CRLF}");

        sb.Append($"Amostras: {sampleCount} ({options.SamplesPerCycle} por ciclo){CRLF}");
        sb.Append($"Taxa de amostragem: {sampleRateHz} Sa/s{CRLF}");
        sb.Append($"Frequência nominal: {WaveformSynthesizer.LINE_FREQUENCY} Hz{CRLF}");
        sb.Append($"Formato: {FormatName(options.DataFormat)}{CRLF}");
        return sb.ToString();
    }

    private static string BuildCfg(
        string recordName,
        DateTime timestamp,
        ComtradeWaveformOptions options,
        int sampleRateHz,
        IReadOnlyList<DevicePoint> analogPoints,
        IReadOnlyList<DevicePoint> digitalPoints,
        double[] scaleA,
        DatResult dat,
        int[] normalStates)
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
            int minInt = dat.SampleCount > 0 ? dat.MinStored[i] : 0;
            int maxInt = dat.SampleCount > 0 ? dat.MaxStored[i] : 0;

            string chId = ExtractChannelId(p.Reference);
            string phase = ExtractPhase(p.Reference);
            string unit = GetUnit(p.DataObject);

            sb.Append(string.Format(ci,
                "{0},{1},{2},,{3},{4:G8},{5:G8},0,{6},{7},1,1,P{8}",
                i + 1, chId, phase, unit, scaleA[i], 0.0, minInt, maxInt, CRLF));
        }

        // Definição dos canais digitais
        // Formato: dn,ch_id,ph,ccbm,y
        for (int i = 0; i < digitalCount; i++)
        {
            string chId = ExtractChannelId(digitalPoints[i].Reference);
            sb.Append(string.Format(ci, "{0},{1},,,{2}{3}", i + 1, chId, normalStates[i], CRLF));
        }

        // Frequência nominal da linha (Hz)
        sb.Append(string.Format(ci, "{0}{1}", WaveformSynthesizer.LINE_FREQUENCY, CRLF));

        // Número de taxas de amostragem
        sb.Append($"1{CRLF}");

        // Taxa de amostragem e amostra final
        sb.Append(string.Format(ci, "{0},{1}{2}", sampleRateHz, dat.SampleCount, CRLF));

        // Instante de início do registro
        sb.Append($"{timestamp.ToString("dd/MM/yyyy,HH:mm:ss.ffffff", ci)}{CRLF}");

        // Instante de disparo (início da falta = pré-falta após o início)
        var triggerTime = timestamp.AddMilliseconds(options.PreFaultMs);
        sb.Append($"{triggerTime.ToString("dd/MM/yyyy,HH:mm:ss.ffffff", ci)}{CRLF}");

        // Tipo do arquivo de dados
        sb.Append($"{FormatName(options.DataFormat)}{CRLF}");

        // Multiplicador de tempo (1 = microsegundos)
        sb.Append($"1{CRLF}");

        return sb.ToString();
    }

    // ── Auxiliares ────────────────────────────────────────────────────────

    private static string FormatName(ComtradeDataFormat format) =>
        format == ComtradeDataFormat.Binary ? "BINARY" : "ASCII";

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
