namespace Iec61850Sim.Core.Comtrade.Models;

public class ComtradeRecord
{
    public required string RecordName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required int AnalogChannelCount { get; init; }
    public required int DigitalChannelCount { get; init; }

    /// <summary>Caminho completo do arquivo gerado (.zip ou primeiro .hdr).</summary>
    public required string FilePath { get; init; }
}