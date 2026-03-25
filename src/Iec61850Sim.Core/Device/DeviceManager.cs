namespace Iec61850Sim.Core.Device;

public class DeviceManager
{
    private readonly List<DeviceBase> devices = new();

    public IReadOnlyList<DeviceBase> Devices => devices;

    public List<XCBR> Breakers { get; } = new();

    public List<CSWI> Controllers { get; } = new();

    public List<MeasurementDevice> Measurements { get; } = new();

    public List<LLN0> LLN0Devices { get; } = new();

    public List<LPHD> LPHDDevices { get; } = new();

    public List<CILO> CILOs { get; } = new();

    public List<XSWI> Switches { get; } = new();

    public void Register(DeviceBase device)
    {
        devices.Add(device);

        switch (device)
        {
            case XCBR b:
                Breakers.Add(b);
                break;

            case XSWI s:
                Switches.Add(s);
                break;

            case MeasurementDevice m:
                Measurements.Add(m);
                break;

            case LLN0 lln0:
                LLN0Devices.Add(lln0);
                break;

            case LPHD lphd:
                LPHDDevices.Add(lphd);
                break;
        }
    }

    public void RegisterController(CSWI cswi)
    {
        Controllers.Add(cswi);
    }

    public void RegisterCilo(CILO cilo)
    {
        devices.Add(cilo);
        CILOs.Add(cilo);
    }

    public void Clear()
    {
        devices.Clear();
        Breakers.Clear();
        Controllers.Clear();
        Measurements.Clear();
        LLN0Devices.Clear();
        LPHDDevices.Clear();
        CILOs.Clear();
        Switches.Clear();
    }

    public void Step(double dt)
    {
        foreach (var d in devices)
            d.Step(dt);
    }
}