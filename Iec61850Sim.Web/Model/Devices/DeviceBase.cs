namespace Iec61850Sim.Web.Model.Devices;

public abstract class DeviceBase
{
    public string Name { get; }

    protected DeviceBase(string name)
    {
        Name = name;
    }

    public abstract void Step(double dt);
}