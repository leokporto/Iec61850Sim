using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public interface IModelNode
{
    string Name { get; }
    IReadOnlyList<IModelNode> Children { get; }
    IEnumerable<DevicePoint> GetPoints();
}
