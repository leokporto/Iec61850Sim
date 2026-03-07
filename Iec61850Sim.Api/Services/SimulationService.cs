using Iec61850Sim.Api.Core;

namespace Iec61850Sim.Api.Services;

public class SimulationService : BackgroundService
{
    private readonly SimulationEngine engine;

    public SimulationService(SimulationEngine engine)
    {
        this.engine = engine;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            engine.Step(0.1);

            await Task.Delay(100);
        }
    }
}