namespace Iec61850Sim.Api.Model.Devices;

public abstract class DeviceBase
{
    public string Name { get; }

    protected DeviceBase(string name)
    {
        Name = name;
    }

    public abstract void Step(double dt);
}