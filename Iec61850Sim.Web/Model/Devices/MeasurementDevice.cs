namespace Iec61850Sim.Web.Model.Devices;

public class MeasurementDevice : DeviceBase
{
    private readonly List<DevicePoint> measurements;

    public MeasurementDevice(string name, IEnumerable<DevicePoint> points)
        : base(name)
    {
        measurements = points.ToList();
    }

    
    
    public override void Step(double dt)
    {
        foreach (var p in measurements)
        {
            p.Value = 100 + Random.Shared.NextDouble() * 10;
            p.Timestamp = DateTimeOffset.Now.ToUniversalTime();
            
            //  Console.WriteLine($"{p.Reference} = {p.Value}");
        }
    }
}