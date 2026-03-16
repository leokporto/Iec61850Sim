namespace Iec61850Sim.Core.Model.Devices;

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
        var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        foreach (var p in measurements)
        {
            double baseValue = 100;
            double oscillation = Math.Sin(t / 10) * 5;
            double noise = (Random.Shared.NextDouble() - 0.5) * 0.5;

            p.Value = baseValue + oscillation + noise;
            p.Timestamp = DateTimeOffset.UtcNow;
        }

        //Console.WriteLine("MeasurementDevice '{Name}' updated {measurements.Count} points at time {t:F2}s");
    }
}