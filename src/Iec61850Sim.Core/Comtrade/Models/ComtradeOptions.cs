using System.IO.Compression;

namespace Iec61850Sim.Core.Comtrade.Models;

public record ComtradeOptions
{
    public bool GenerateZip { get; init; } = true;

    /// <summary>
    /// Nível de compressão das entradas do .zip. <see cref="CompressionLevel.NoCompression"/>
    /// mantém o .zip com tamanho próximo à soma dos arquivos.
    /// </summary>
    public CompressionLevel ZipCompression { get; init; } = CompressionLevel.Optimal;

    public ComtradeWaveformOptions Waveform { get; init; } = new();
}
