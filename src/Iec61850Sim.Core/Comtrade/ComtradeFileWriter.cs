using System.IO.Compression;
using System.Text;

namespace Iec61850Sim.Core.Comtrade;

/// <summary>
/// Grava o conteúdo COMTRADE em disco como .zip ou como três arquivos separados.
/// O diretório de saída é criado automaticamente se não existir.
/// </summary>
internal static class ComtradeFileWriter
{
    internal static string Write(
        string recordName,
        ComtradeGenerator.GeneratedContent content,
        string outputDirectory,
        bool generateZip)
    {
        Directory.CreateDirectory(outputDirectory);

        if (generateZip)
            return WriteZip(recordName, content, outputDirectory);

        return WriteSeparateFiles(recordName, content, outputDirectory);
    }

    private static string WriteZip(
        string recordName,
        ComtradeGenerator.GeneratedContent content,
        string outputDirectory)
    {
        var zipPath = Path.Combine(outputDirectory, $"{recordName}.zip");

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddTextEntry(zip, $"{recordName}.hdr", content.Hdr);
        AddTextEntry(zip, $"{recordName}.cfg", content.Cfg);
        AddTextEntry(zip, $"{recordName}.dat", content.Dat);

        return zipPath;
    }

    private static string WriteSeparateFiles(
        string recordName,
        ComtradeGenerator.GeneratedContent content,
        string outputDirectory)
    {
        var hdrPath = Path.Combine(outputDirectory, $"{recordName}.hdr");
        var cfgPath = Path.Combine(outputDirectory, $"{recordName}.cfg");
        var datPath = Path.Combine(outputDirectory, $"{recordName}.dat");

        // COMTRADE utiliza codificação ASCII
        File.WriteAllText(hdrPath, content.Hdr, Encoding.ASCII);
        File.WriteAllText(cfgPath, content.Cfg, Encoding.ASCII);
        File.WriteAllText(datPath, content.Dat, Encoding.ASCII);

        return hdrPath;
    }

    private static void AddTextEntry(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open(), Encoding.ASCII);
        writer.Write(content);
    }
}