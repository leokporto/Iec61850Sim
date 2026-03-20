using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Model.Tree;

public interface IModelNode
{
    string Name { get; }
    IReadOnlyList<IModelNode> Children { get; }

    /// <summary>
    /// Returns the PointValue objects owned by this node (snapshot at tree-build time).
    /// Values are NOT refreshed — use GetLivePoints for current simulation state.
    /// </summary>
    IEnumerable<PointValue<object>> GetPoints();

    /// <summary>
    /// Returns live PointValues by resolving each leaf's reference from the registry,
    /// updating Value, Timestamp, and Quality in-place on the stored PointValue instance.
    /// </summary>
    IEnumerable<PointValue<object>> GetLivePoints(IPointRegistry registry);
}