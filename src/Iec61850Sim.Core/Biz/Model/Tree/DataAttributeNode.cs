using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public sealed class DataAttributeNode : ModelNodeBase
{
    public DevicePoint Point { get; }

    public DataAttributeNode(DevicePoint point) : base(point.Fc.ToString())
    {
        Point = point;
    }

    public override IEnumerable<DevicePoint> GetPoints() => [Point];
}
