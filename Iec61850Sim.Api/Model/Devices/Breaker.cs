namespace Iec61850Sim.Api.Model.Devices;

public class Breaker : DeviceBase
{
    public DevicePoint Position { get; }

    public Breaker(string name, DevicePoint pos) : base(name)
    {
        Position = pos;
    }

    public void Open() => Position.Value = 1;

    public void Close() => Position.Value = 2;

    public override void Step(double dt) {}
}