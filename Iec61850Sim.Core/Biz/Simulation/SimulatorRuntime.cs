using IEC61850.Server;
using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Iec61850;

namespace Iec61850Sim.Core.Biz.Simulation;

public class SimulatorRuntime
{
    private readonly SimulationEngine simulation;
    private readonly IecServerHost iecHost;
    private readonly DeviceManager devices;
    private readonly IedModel model;

    public bool Running { get; private set; }

    public SimulatorRuntime(
        IedModel model,
        DeviceManager devices,
        SimulationEngine simulation,
        IecServerHost iecHost)
    {
        this.model = model;
        this.devices = devices;
        this.simulation = simulation;
        this.iecHost = iecHost;
    }

    public void Start()
    {
        if (Running)
            return;

        iecHost.Start(102);

        var binder = new PointBinder();
        binder.Bind(model, devices, iecHost.Server);

        Running = true;

        Console.WriteLine("Simulation started");
    }

    public void Stop()
    {
        if (!Running)
            return;

        Running = false;

        iecHost.Stop();

        Console.WriteLine("Simulation stopped");
    }

    public void Step(double dt)
    {
        simulation.Step(dt);
        iecHost.Publish();
    }
}
