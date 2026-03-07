using Iec61850Sim.Api.Model;
using Iec61850Sim.Api.Model.Devices;

namespace Iec61850Sim.Api.Core;

public class SimulationEngine
{
    private readonly SimulationClock clock;

    private readonly List<DeviceBase> devices = new();

    public SimulationEngine(SimulationClock clock)
    {
        this.clock = clock;
    }

    public void AddDevice(DeviceBase device)
    {
        devices.Add(device);

        clock.Register(device.Step);
    }

    public void Step(double dt)
    {
        clock.Tick(dt);
    }
}