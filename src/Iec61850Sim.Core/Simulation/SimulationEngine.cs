using Iec61850Sim.Core.Device;

namespace Iec61850Sim.Core.Simulation;

public class SimulationEngine
{
    private readonly SimulationClock clock;
    private readonly DeviceManager deviceManager;

    public SimulationEngine(SimulationClock clock, DeviceManager deviceManager)
    {
        this.clock = clock;
        this.deviceManager = deviceManager;

        RegisterDevices();
    }

    private void RegisterDevices()
    {
        foreach (var device in deviceManager.Devices)
        {
            clock.Register(device.Step);
        }
    }

    public void Step(double dt)
    {
        clock.Tick(dt);
    }
}