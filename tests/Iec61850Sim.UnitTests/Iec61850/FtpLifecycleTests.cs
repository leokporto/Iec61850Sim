using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Scl;

using NSubstitute;

using Xunit;

namespace Iec61850Sim.UnitTests.Iec61850;

public class FtpLifecycleTests
{
    [Fact]
    public async Task ApplyAsync_WhenFtpAndNotRunning_StartsServer()
    {
        var host = Substitute.For<IFtpServerHost>();
        host.IsRunning.Returns(false);

        await FtpLifecycle.ApplyAsync(FileHandlingCapability.Ftp, host);

        await host.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await host.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_WhenBothAndNotRunning_StartsServer()
    {
        var host = Substitute.For<IFtpServerHost>();
        host.IsRunning.Returns(false);

        await FtpLifecycle.ApplyAsync(FileHandlingCapability.Both, host);

        await host.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await host.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_WhenMmsAndRunning_StopsServer()
    {
        var host = Substitute.For<IFtpServerHost>();
        host.IsRunning.Returns(true);

        await FtpLifecycle.ApplyAsync(FileHandlingCapability.Mms, host);

        await host.Received(1).StopAsync(Arg.Any<CancellationToken>());
        await host.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_WhenNoneAndRunning_StopsServer()
    {
        var host = Substitute.For<IFtpServerHost>();
        host.IsRunning.Returns(true);

        await FtpLifecycle.ApplyAsync(FileHandlingCapability.None, host);

        await host.Received(1).StopAsync(Arg.Any<CancellationToken>());
        await host.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_WhenMmsAndNotRunning_DoesNothing()
    {
        var host = Substitute.For<IFtpServerHost>();
        host.IsRunning.Returns(false);

        await FtpLifecycle.ApplyAsync(FileHandlingCapability.Mms, host);

        await host.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await host.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_WhenFtpAndAlreadyRunning_DoesNothing()
    {
        var host = Substitute.For<IFtpServerHost>();
        host.IsRunning.Returns(true);

        await FtpLifecycle.ApplyAsync(FileHandlingCapability.Ftp, host);

        await host.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await host.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
    }
}
