using System.Xml.Linq;

using Iec61850Sim.Core.Scl;

using Xunit;

namespace Iec61850Sim.UnitTests.Scl;

public class SclConversionServiceTests
{
    // ── ParseFileHandlingCapability ───────────────────────────────────────

    [Fact]
    public void ParseFileHandlingCapability_WhenElementAbsent_ReturnsNone()
    {
        var doc = BuildSclDoc(fileHandlingElement: null);

        var result = SclConversionService.ParseFileHandlingCapability(doc);

        Assert.Equal(FileHandlingCapability.None, result);
    }

    [Fact]
    public void ParseFileHandlingCapability_WhenElementPresentWithNoAttributes_ReturnsMms()
    {
        var doc = BuildSclDoc(fileHandlingElement: new XElement("FileHandling"));

        var result = SclConversionService.ParseFileHandlingCapability(doc);

        Assert.Equal(FileHandlingCapability.Mms, result);
    }

    [Fact]
    public void ParseFileHandlingCapability_WhenMmsTrueOnly_ReturnsMms()
    {
        var doc = BuildSclDoc(fileHandlingElement: new XElement("FileHandling",
            new XAttribute("mms", "true")));

        var result = SclConversionService.ParseFileHandlingCapability(doc);

        Assert.Equal(FileHandlingCapability.Mms, result);
    }

    [Fact]
    public void ParseFileHandlingCapability_WhenFtpTrueOnly_ReturnsFtp()
    {
        var doc = BuildSclDoc(fileHandlingElement: new XElement("FileHandling",
            new XAttribute("ftp", "true")));

        var result = SclConversionService.ParseFileHandlingCapability(doc);

        Assert.Equal(FileHandlingCapability.Ftp, result);
    }

    [Fact]
    public void ParseFileHandlingCapability_WhenMmsAndFtpBothTrue_ReturnsBoth()
    {
        var doc = BuildSclDoc(fileHandlingElement: new XElement("FileHandling",
            new XAttribute("mms", "true"),
            new XAttribute("ftp", "true")));

        var result = SclConversionService.ParseFileHandlingCapability(doc);

        Assert.Equal(FileHandlingCapability.Both, result);
    }

    [Fact]
    public void ParseFileHandlingCapability_WithSclNamespace_FindsElementCorrectly()
    {
        XNamespace ns = "http://www.iec.ch/61850/2003/SCL";
        var doc = new XDocument(
            new XElement(ns + "SCL",
                new XElement(ns + "IED", new XAttribute("name", "Demo"),
                    new XElement(ns + "Services",
                        new XElement(ns + "FileHandling")))));

        var result = SclConversionService.ParseFileHandlingCapability(doc);

        Assert.Equal(FileHandlingCapability.Mms, result);
    }

    [Fact]
    public void ParseFileHandlingCapability_AttributeCaseInsensitive_ReturnsMms()
    {
        var doc = BuildSclDoc(fileHandlingElement: new XElement("FileHandling",
            new XAttribute("mms", "TRUE")));

        var result = SclConversionService.ParseFileHandlingCapability(doc);

        Assert.Equal(FileHandlingCapability.Mms, result);
    }

    // ── GetFileHandlingCapability (no prior ConvertToCfg) ─────────────────

    [Fact]
    public void GetFileHandlingCapability_WhenNoSclParsed_ReturnsMmsDefault()
    {
        var sut = new SclConversionService(Microsoft.Extensions.Logging.Abstractions.NullLogger<SclConversionService>
            .Instance);

        var result = sut.GetFileHandlingCapability();

        Assert.Equal(FileHandlingCapability.Mms, result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static XDocument BuildSclDoc(XElement? fileHandlingElement)
    {
        var services = new XElement("Services");
        if (fileHandlingElement != null)
            services.Add(fileHandlingElement);

        return new XDocument(
            new XElement("SCL",
                new XElement("IED", new XAttribute("name", "Demo"),
                    services)));
    }
}
