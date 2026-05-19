namespace Iec61850Sim.Core.Scl;

public sealed class ServerCapabilities
{
    public FileHandlingCapability FileHandling
    {
        get;
        set;
    } = FileHandlingCapability.Mms;
}
