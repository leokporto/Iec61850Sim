namespace Iec61850Sim.Api.Model.Devices;

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
            
            //Console.WriteLine($"{p.Reference} = {p.Value}");
        }
    }
}