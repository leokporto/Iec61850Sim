using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

public class Breaker : DeviceBase
{
    private readonly IPointRegistry _registry;

    public string PositionReference { get; }
    public string EquipmentName { get; }

    public Breaker(string name, string positionReference, string equipmentName, IPointRegistry registry)
        : base(name)
    {
        PositionReference = positionReference;
        EquipmentName = equipmentName;
        _registry = registry;
    }

    public void Open() => _registry.SetValue(new PointValue<object>
    {
        Reference = PositionReference,
        Value = 1,
        Timestamp = DateTimeOffset.UtcNow,
        Quality = 192
    });

    public void Close() => _registry.SetValue(new PointValue<object>
    {
        Reference = PositionReference,
        Value = 2,
        Timestamp = DateTimeOffset.UtcNow,
        Quality = 192
    });

    public override void Step(double dt) { }
}