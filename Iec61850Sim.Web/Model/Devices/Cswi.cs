namespace Iec61850Sim.Web.Model.Devices;

/// <summary>
/// Switch controller (CSWI on IEC61850).
/// The logical controller that is responsible for the commands and associated to a Breaker Status (XCBR).
/// </summary>
public class Cswi : DeviceBase
{
    public Cswi(string name, Breaker breaker) : base(name)
    {
        Breaker = breaker;
    }

    /// <summary>
    /// The breaker (XCBR) that is controlled by this CSWI
    /// </summary>
    public Breaker Breaker { get; private set; }

    public void BindBreaker(Breaker breaker)
    {
        Breaker = breaker;
    }

    public void Operate(bool close)
    {
        if(close)
            Breaker.Close();
        else
            Breaker.Open();
    }

    public override void Step(double dt)
    {
        
    }
}