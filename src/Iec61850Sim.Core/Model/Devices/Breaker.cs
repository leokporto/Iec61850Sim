using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Model.Devices;

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
