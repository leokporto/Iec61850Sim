namespace Iec61850Sim.Core.Scl;

public interface ISclConversionService
{
    string GetIedName();

    IList<string> GetLogicalDeviceNames();

    /// <summary>
    /// Converts an SCL file (.icd/.cid/.scd) to a CFG file.
    /// Returns the absolute path to the generated CFG file.
    /// </summary>
    string ConvertToCfg(string sclFilePath);
}
