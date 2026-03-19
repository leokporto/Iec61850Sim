using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public sealed class DataObjectNode : ModelNodeBase
{
    public DataObjectNode(string name) : base(name) { }

    public override IEnumerable<DevicePoint> GetPoints() =>
        Children.SelectMany(c => c.GetPoints());
}
