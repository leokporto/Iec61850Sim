namespace Iec61850Sim.Core.Device;

public class Switch : DeviceBase
{
    public string PositionReference { get; }

    public Switch(string name, string positionReference) : base(name)
    {
        PositionReference = positionReference;
    }

    public override void Step(double dt) { }
}