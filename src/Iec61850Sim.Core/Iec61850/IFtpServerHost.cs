namespace Iec61850Sim.Core.Iec61850;

public interface IFtpServerHost
{
    bool IsRunning
    {
        get;
    }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
