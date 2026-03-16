namespace Iec61850Sim.Core.Model.Devices;

/// <summary>
/// Switch controller (CSWI on IEC61850).
/// The logical controller that is responsible for the commands and associated to a Breaker Status (XCBR).
/// </summary>
public class Cswi : DeviceBase
{
    public Cswi(string name, DevicePoint cmdController, DevicePoint position, Breaker breaker) : base(name)
    {
        CommandController = cmdController;
        Position = position;
        Breaker = breaker;
    }

    /// <summary>
    /// The breaker (XCBR) that is controlled by this CSWI
    /// </summary>
    public Breaker Breaker { get; private set; }

    /// <summary>
    /// The controller CO point (CSWI.Pos StVal) that is associated to the breaker position point (XCBR.Pos).
    /// </summary>
    public DevicePoint Position { get; }

    /// <summary>
    /// The controller CO point (CSWI.Pos CtlVal) that is used to send commands to the breaker.
    /// </summary>
    public DevicePoint CommandController
    {
        get;
    }

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