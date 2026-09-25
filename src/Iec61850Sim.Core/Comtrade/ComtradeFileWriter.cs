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
        bool generateZip,
        CompressionLevel compression = CompressionLevel.Optimal)
    {
        Directory.CreateDirectory(outputDirectory);

        if (generateZip)
            return WriteZip(recordName, content, outputDirectory, compression);

        return WriteSeparateFiles(recordName, content, outputDirectory);
    }

    private static string WriteZip(
        string recordName,
        ComtradeGenerator.GeneratedContent content,
        string outputDirectory,
        CompressionLevel compression)
    {
        var zipPath = Path.Combine(outputDirectory, $"{recordName}.zip");

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddEntry(zip, $"{recordName}.hdr", Encoding.ASCII.GetBytes(content.Hdr), compression);
        AddEntry(zip, $"{recordName}.cfg", Encoding.ASCII.GetBytes(content.Cfg), compression);
        AddEntry(zip, $"{recordName}.dat", content.Dat, compression);

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

        // COMTRADE utiliza codificação ASCII; o .dat já vem codificado (ASCII ou binário)
        File.WriteAllText(hdrPath, content.Hdr, Encoding.ASCII);
        File.WriteAllText(cfgPath, content.Cfg, Encoding.ASCII);
        File.WriteAllBytes(datPath, content.Dat);

        return hdrPath;
    }

    private static void AddEntry(ZipArchive zip, string entryName, byte[] content, CompressionLevel compression)
    {
        var entry = zip.CreateEntry(entryName, compression);
        using var stream = entry.Open();
        stream.Write(content);
    }
}
