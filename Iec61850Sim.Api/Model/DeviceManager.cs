
using Iec61850Sim.Api.Model.Devices;

namespace Iec61850Sim.Api.Model;

public class DeviceManager
{
    private readonly List<DeviceBase> devices = new();

    public IReadOnlyList<DeviceBase> Devices => devices;

    public List<Breaker> Breakers { get; } = new();

    public List<Cswi> Controllers { get; } = new();

    public List<MeasurementDevice> Measurements { get; } = new();

    public void Register(DeviceBase device)
    {
        devices.Add(device);

        switch (device)
        {
            case Breaker b:
                Breakers.Add(b);
                break;

            case MeasurementDevice m:
                Measurements.Add(m);
                break;
        }
    }

    public void RegisterController(Cswi cswi)
    {
        Controllers.Add(cswi);
    }

    public void Step(double dt)
    {
        foreach (var d in devices)
            d.Step(dt);
    }
}