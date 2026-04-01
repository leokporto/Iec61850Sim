using IEC61850.SCL;

using Microsoft.Extensions.Logging;

namespace Iec61850Sim.Core.Scl;

public class SclConversionService : ISclConversionService
{
    private static readonly string[] SclExtensions = [".icd", ".cid", ".scd"];
    private readonly ILogger<SclConversionService> _logger;
    private string _iedName = "IED";
    private List<string> _logicalDeviceNames = new List<string>();

    public SclConversionService(ILogger<SclConversionService> logger)
    {
        _logger = logger;
    }

    public string GetIedName()
    {
        return _iedName;
    }

    public IList<string> GetLogicalDeviceNames()
    {
        return _logicalDeviceNames ?? new List<string>();
    }

    public string ConvertToCfg(string sclFilePath)
    {
        if (!File.Exists(sclFilePath))
            throw new FileNotFoundException($"SCL file not found: {sclFilePath}", sclFilePath);

        try
        {
            var outputDir = Path.Combine(AppContext.BaseDirectory, "generated");
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(sclFilePath) + ".cfg");

            var sclDocument = new SclDocument(sclFilePath);

            var ied = sclDocument.IEDs.First();
            _iedName = ied.Name;

            var accessPoint = ied.AccessPoints.First();
            var iedModel = sclDocument.GetDataModel(ied.Name, accessPoint.Name);

            _logicalDeviceNames = iedModel.LogicalDevices.Select(ld => ld.Inst).ToList();
            _logicalDeviceNames.Sort();

            if (File.Exists(outputPath) && File.GetLastWriteTime(outputPath) >= File.GetLastWriteTime(sclFilePath))
            {
                _logger.LogInformation("[SclConversionService] Reusing existing CFG: {OutputPath}", outputPath);
                return outputPath;
            }

            _logger.LogInformation("[SclConversionService] SCL detected: {SclPath} → Converting...", sclFilePath);

            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            _ = new DynamicModelGenerator(sclDocument, writer, iedModel, accessPoint);

            _logger.LogInformation("[SclConversionService] CFG generated: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SclConversionService] Conversion failed for: {SclPath}", sclFilePath);
            throw new InvalidOperationException($"Failed to convert SCL file '{sclFilePath}' to CFG: {ex.Message}", ex);
        }
    }

    public static bool IsSclFile(string path) =>
        SclExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
