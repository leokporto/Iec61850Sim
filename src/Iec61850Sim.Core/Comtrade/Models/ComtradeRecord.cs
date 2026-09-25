namespace Iec61850Sim.Core.Comtrade.Models;

public class ComtradeRecord
{
    public required string RecordName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required int AnalogChannelCount { get; init; }
    public required int DigitalChannelCount { get; init; }

    /// <summary>Caminho completo do arquivo gerado (.zip ou primeiro .hdr).</summary>
    public required string FilePath { get; init; }

    /// <summary>Tamanho em disco do arquivo principal: o .zip, ou o .dat quando gerado separado.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>Tamanho do conteúdo .dat (descompactado).</summary>
    public long DatSizeBytes { get; init; }

    public int SampleCount { get; init; }
    public int SampleRateHz { get; init; }
    public ComtradeDataFormat DataFormat { get; init; }
    public ComtradeScenario Scenario { get; init; }
}
