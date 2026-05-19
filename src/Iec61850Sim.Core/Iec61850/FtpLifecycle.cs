using Iec61850Sim.Core.Scl;

namespace Iec61850Sim.Core.Iec61850;

public static class FtpLifecycle
{
    public static async Task ApplyAsync(
        FileHandlingCapability capability,
        IFtpServerHost ftpHost,
        CancellationToken ct = default)
    {
        bool needsFtp = capability is FileHandlingCapability.Ftp
            or FileHandlingCapability.Both;

        if (needsFtp && !ftpHost.IsRunning)
            await ftpHost.StartAsync(ct);
        else if (!needsFtp && ftpHost.IsRunning)
            await ftpHost.StopAsync(ct);
    }
}
