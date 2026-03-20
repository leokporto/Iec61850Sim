using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

public class MeasurementDevice : DeviceBase
{
    private readonly List<string> _references;
    private readonly IPointRegistry _registry;

    public MeasurementDevice(string name, IEnumerable<string> references, IPointRegistry registry)
        : base(name)
    {
        _references = references.ToList();
        _registry = registry;
    }

    public override void Step(double dt)
    {
        var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var now = DateTimeOffset.UtcNow;

        var updates = _references.Select(r =>
        {
            double baseValue = 100;
            double oscillation = Math.Sin(t / 10) * 5;
            double noise = (Random.Shared.NextDouble() - 0.5) * 0.5;

            return new PointValue<object>
            {
                Reference = r,
                Value = baseValue + oscillation + noise,
                Timestamp = now,
                Quality = 192
            };
        });

        _registry.SetValues(updates);

        Console.WriteLine($"MeasurementDevice '{Name}' updated {_references.Count} points at time {t:F2}s");
    }
}