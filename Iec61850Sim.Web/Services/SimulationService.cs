using IEC61850.Server;
using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Biz.Simulation;
using Iec61850Sim.Core.Iec61850;

namespace Iec61850Sim.Web.Services;

public class SimulationService : BackgroundService
{
    private readonly SimulationEngine simulation;
    private readonly IecServerHost iecHost;
    private readonly DeviceManager deviceManager;
    private readonly PointRegistry registry;
    private readonly IedModel model;

    public SimulationService(IedModel model,
        PointRegistry registry,
        DeviceManager deviceManager,
        SimulationEngine simulation,
        IecServerHost iecHost)
    {
        this.model = model;
        this.registry = registry;
        this.deviceManager = deviceManager;
        this.simulation = simulation;
        this.iecHost = iecHost;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        iecHost.Start(102);

        var binder = new PointBinder();
        binder.Bind(model, deviceManager, iecHost.Server);
        
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Simulation service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            simulation.Step(0.1);

            iecHost.Publish();

            await Task.Delay(2000, stoppingToken);
        }
    }
}