using IEC61850.Common;

using Iec61850Sim.Core.Comtrade.Models;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Points;

using Microsoft.Extensions.Logging;

namespace Iec61850Sim.Core.Comtrade;

public class ComtradeService : IComtradeService
{
    private readonly IPointRegistry _pointRegistry;
    private readonly ILogger<ComtradeService> _logger;
    private readonly string _fileStoreRoot;
    private readonly List<ComtradeRecord> _records = [];

    // Construtor usado pela injeção de dependência
    public ComtradeService(IPointRegistry pointRegistry, ILogger<ComtradeService> logger)
        : this(pointRegistry, logger, Path.Combine(AppContext.BaseDirectory, "FileStore"))
    {
    }

    // Construtor com raiz FileStore configurável (utilizado em testes)
    public ComtradeService(IPointRegistry pointRegistry, ILogger<ComtradeService> logger, string fileStoreRoot)
    {
        _pointRegistry = pointRegistry;
        _logger = logger;
        _fileStoreRoot = fileStoreRoot;
    }

    public async Task<ComtradeRecord> GenerateFaultAsync(ComtradeOptions options)
    {
        return await Task.Run(() =>
        {
            var timestamp = DateTime.UtcNow;
            var recordName = $"Fault_{timestamp:yyyyMMdd_HHmmssfff}";

            // Canais analógicos: pontos MMXU com FC = MX
            var analogPoints = _pointRegistry.GetByFc(FunctionalConstraint.MX)
                .Where(p => p.LnType == eLnType.MMXU)
                .ToList();

            // Canais digitais: stVal de Pos em XCBR/XSWI com FC = ST
            var digitalPoints = _pointRegistry.GetByFc(FunctionalConstraint.ST)
                .Where(p => (p.LnType == eLnType.XCBR || p.LnType == eLnType.XSWI)
                             && p.DataObject == "Pos")
                .ToList();

            // Deriva ldName dos pontos digitais (XCBR/XSWI); usa analógicos como fallback.
            // Conforme IEC 61850-8-1 cláusula 23.1, arquivos COMTRADE ficam em
            // FileStore/LD/<ldName>/COMTRADE/ sob cada LD que originou os canais.
            var ldName = digitalPoints.FirstOrDefault()?.LogicalDevice
                      ?? analogPoints.FirstOrDefault()?.LogicalDevice
                      ?? "DEFAULT";

            var outputDirectory = Path.Combine(_fileStoreRoot, "LD", ldName, "COMTRADE");

            try
            {
                var content = ComtradeGenerator.Generate(recordName, timestamp, analogPoints, digitalPoints);
                var filePath = ComtradeFileWriter.Write(recordName, content, outputDirectory, options.GenerateZip);

                var record = new ComtradeRecord
                {
                    RecordName = recordName,
                    Timestamp = timestamp,
                    AnalogChannelCount = analogPoints.Count,
                    DigitalChannelCount = digitalPoints.Count,
                    FilePath = filePath
                };

                lock (_records)
                    _records.Add(record);

                _logger.LogInformation("COMTRADE gerado: {RecordName} → {FilePath}", recordName, filePath);
                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gerar registro COMTRADE '{RecordName}'", recordName);
                throw;
            }
        });
    }

    public IReadOnlyList<ComtradeRecord> GetGeneratedRecords()
    {
        lock (_records)
            return _records.AsReadOnly();
    }
}