using Iec61850Sim.Core.Biz;

namespace Iec61850Sim.Web.Services;

public class SimulationService : BackgroundService
{
    private readonly RuntimeEngine runtime;
    private readonly int _interval = 2000;

    public SimulationService(RuntimeEngine runtime)
    {
        this.runtime = runtime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Simulation service ready");

        var simulationLoop = SimulationLoop(stoppingToken);
        var publishLoop = PublishLoop(stoppingToken);

        await Task.WhenAll(simulationLoop, publishLoop);
    }

    private async Task SimulationLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (runtime.SimulationRunning)
            {
                runtime.Step(0.1);
            }

            await Task.Delay(_interval, token);
        }
    }

    private async Task PublishLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (runtime.ServerRunning)
            {
                runtime.Publish();
            }

            await Task.Delay(_interval, token);
        }
    }
}