using Iec61850Sim.Api.Model;

namespace Iec61850Sim.Api.Model.Devices;

public class Switch : DeviceBase
{
    public DevicePoint Position { get; }

    public Switch(string name, DevicePoint pos) : base(name)
    {
        Position = pos;
    }

    public override void Step(double dt) {}
}