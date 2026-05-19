using FubarDev.FtpServer;

using CoreFtpServerHost = Iec61850Sim.Core.Iec61850.IFtpServerHost;

namespace Iec61850Sim.Web.Services;

public sealed class FtpServerHost : CoreFtpServerHost, IAsyncDisposable
{
    private readonly IFtpServer _ftpServer;
    private bool _isRunning;

    public FtpServerHost(IFtpServer ftpServer)
    {
        _ftpServer = ftpServer;
    }

    public bool IsRunning => _isRunning;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            return Task.CompletedTask;

        _ftpServer.Start();
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            return Task.CompletedTask;

        _ftpServer.Stop();
        _isRunning = false;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
            await StopAsync();
    }
}
