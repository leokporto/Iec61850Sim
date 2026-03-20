namespace Iec61850Sim.Core.Device;

public class XSWI : DeviceBase
{
    public string PositionReference { get; }

    public XSWI(string name, string positionReference) : base(name)
    {
        PositionReference = positionReference;
    }

    public override void Step(double dt) { }
}
