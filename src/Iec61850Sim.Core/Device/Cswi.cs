using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

/// <summary>
/// Switch controller (CSWI on IEC61850).
/// Responsible for commands; delegates physical switching to the associated Breaker (XCBR).
/// </summary>
public class Cswi : DeviceBase
{
    private readonly IPointRegistry _registry;

    public Cswi(string name, string commandReference, string positionReference, Breaker breaker, IPointRegistry registry)
        : base(name)
    {
        CommandReference = commandReference;
        PositionReference = positionReference;
        Breaker = breaker;
        _registry = registry;
    }

    /// <summary>The breaker (XCBR) controlled by this CSWI.</summary>
    public Breaker Breaker { get; private set; }

    /// <summary>Reference of the CO point used to receive commands (CSWI.Pos.Oper.ctlVal).</summary>
    public string CommandReference { get; }

    /// <summary>Reference of the ST point mirroring the breaker position (CSWI.Pos.stVal).</summary>
    public string PositionReference { get; }

    public void BindBreaker(Breaker breaker) => Breaker = breaker;

    public void Operate(bool close)
    {
        if (close) Breaker.Close();
        else Breaker.Open();
    }

    public override void Step(double dt) { }
}