namespace Iec61850Sim.Core.Model.Devices;

public class Switch : DeviceBase
{
    public DevicePoint Position { get; }

    public Switch(string name, DevicePoint pos) : base(name)
    {
        Position = pos;
    }

    public override void Step(double dt) {}
}