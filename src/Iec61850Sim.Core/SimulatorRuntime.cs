using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Simulation;

namespace Iec61850Sim.Core;

public class RuntimeEngine
{
    private readonly SimulationEngine simulation;
    private readonly IecServerHost iecHost;
    private readonly DeviceManager devices;
    private readonly IedModel model;

    public bool SimulationRunning { get; private set; }
    public bool ServerRunning { get; private set; }


    public RuntimeEngine(
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

    public void StartServer()
    {
        if (ServerRunning)
            return;

        iecHost.Start(102);

        ServerRunning = true;

        InitializeStaticDevices();

        Console.WriteLine("IEC server started");
    }

    private void InitializeStaticDevices()
    {
        foreach (var lln0 in devices.LLN0Devices)
            lln0.Initialize(iecHost.ReadAttributeValue);

        foreach (var lphd in devices.LPHDDevices)
            lphd.Initialize(iecHost.ReadAttributeValue);

        iecHost.Publish();
    }

    public void StopServer()
    {
        if (!ServerRunning)
            return;

        iecHost.Stop();

        ServerRunning = false;

        Console.WriteLine("IEC server stopped");
    }

    public void StartSimulation()
    {
        if (!ServerRunning)
            StartServer();

        SimulationRunning = true;

        Console.WriteLine("Simulation started");
    }

    public void StopSimulation()
    {
        SimulationRunning = false;

        Console.WriteLine("Simulation stopped (server still running)");
    }

    public void Step(double dt)
    {
        simulation.Step(dt);
    }

    public void Publish()
    {
        iecHost.Publish();
    }

}