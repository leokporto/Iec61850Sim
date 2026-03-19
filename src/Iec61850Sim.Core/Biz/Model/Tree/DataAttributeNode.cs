using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public sealed class DataAttributeNode : ModelNodeBase
{
    private readonly string _reference;

    /// <summary>
    /// The PointValue owned by this leaf node.
    /// Reference and FC are set at build time.
    /// Value, Timestamp, and Quality are refreshed on each GetLivePoints call.
    /// </summary>
    public PointValue<object> Point { get; }

    public DataAttributeNode(DevicePoint point) : base(point.Fc.ToString())
    {
        _reference = point.Reference;
        Point = new PointValue<object>
        {
            Reference = point.Reference,
            FC = point.Fc
        };
    }

    public override IEnumerable<PointValue<object>> GetPoints() => [Point];

    public override IEnumerable<PointValue<object>> GetLivePoints(IPointRegistry registry)
    {
        var live = registry.GetValue<object>(_reference);
        Point.Value = live.Value;
        Point.Timestamp = live.Timestamp;
        Point.Quality = live.Quality;
        return [Point];
    }
}
