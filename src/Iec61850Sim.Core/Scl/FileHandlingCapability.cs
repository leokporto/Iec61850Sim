namespace Iec61850Sim.Core.Scl;

public enum FileHandlingCapability
{
    None, // <FileHandling> absent — no file transfer supported
    Mms, // <FileHandling> present with no attributes, or mms="true"
    Ftp, // <FileHandling ftp="true"/> only
    Both // <FileHandling mms="true" ftp="true"/>
}
