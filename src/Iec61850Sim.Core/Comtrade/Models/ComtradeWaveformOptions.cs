namespace Iec61850Sim.Core.Comtrade.Models;

/// <summary>
/// Parâmetros da forma de onda e do tamanho do registro COMTRADE.
/// </summary>
public record ComtradeWaveformOptions
{
    public const int MIN_SAMPLES_PER_CYCLE = 4;
    public const int MAX_SAMPLES_PER_CYCLE = 512;
    public const int MAX_SEGMENT_MS = 60_000;
    public const long MAX_TARGET_SIZE_BYTES = 50L * 1024 * 1024;

    public int SamplesPerCycle { get; init; } = 16;
    public int PreFaultMs { get; init; } = 100;
    public int FaultMs { get; init; } = 200;

    /// <summary>Ignorado quando <see cref="TargetSizeBytes"/> está definido.</summary>
    public int PostFaultMs { get; init; } = 100;

    /// <summary>Tempo morto entre abertura e religamento (somente <see cref="ComtradeScenario.AutoReclose"/>).</summary>
    public int DeadTimeMs { get; init; } = 300;

    public ComtradeScenario Scenario { get; init; } = ComtradeScenario.SinglePhaseToGround;
    public ComtradeDataFormat DataFormat { get; init; } = ComtradeDataFormat.Ascii;

    /// <summary>Adiciona 3ª e 5ª harmônicas e ruído determinístico.</summary>
    public bool HarmonicsAndNoise { get; init; } = true;

    /// <summary>
    /// Tamanho desejado do arquivo .dat, em bytes. Quando definido, o pós-falta é estendido
    /// até o maior número de amostras cujo .dat não ultrapassa este valor.
    /// </summary>
    public long? TargetSizeBytes { get; init; }

    /// <summary>Retorna a lista de erros de validação (vazia quando as opções são válidas).</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (SamplesPerCycle is < MIN_SAMPLES_PER_CYCLE or > MAX_SAMPLES_PER_CYCLE)
            errors.Add($"Amostras por ciclo deve estar entre {MIN_SAMPLES_PER_CYCLE} e {MAX_SAMPLES_PER_CYCLE}.");

        if (FaultMs is <= 0 or > MAX_SEGMENT_MS)
            errors.Add($"Duração da falta deve estar entre 1 e {MAX_SEGMENT_MS} ms.");

        foreach (var (name, value) in new[]
                 {
                     ("pré-falta", PreFaultMs), ("pós-falta", PostFaultMs), ("tempo morto", DeadTimeMs)
                 })
        {
            if (value is < 0 or > MAX_SEGMENT_MS)
                errors.Add($"Duração de {name} deve estar entre 0 e {MAX_SEGMENT_MS} ms.");
        }

        if (TargetSizeBytes is { } target && (target <= 0 || target > MAX_TARGET_SIZE_BYTES))
            errors.Add($"Tamanho alvo deve estar entre 1 e {MAX_TARGET_SIZE_BYTES} bytes.");

        return errors;
    }
}
