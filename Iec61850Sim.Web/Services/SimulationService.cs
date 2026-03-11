using Iec61850Sim.Core.Biz.Simulation;

namespace Iec61850Sim.Web.Services;

public class SimulationService : BackgroundService
{
    private readonly SimulatorRuntime runtime;

    public SimulationService(SimulatorRuntime runtime)
    {
        this.runtime = runtime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Simulation service ready");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (runtime.Running)
            {
                runtime.Step(0.1);
            }

            await Task.Delay(2000, stoppingToken);
        }
    }
}