using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public sealed class LogicalNodeNode : ModelNodeBase
{
    public LogicalNodeNode(string name) : base(name) { }

    public override IEnumerable<DevicePoint> GetPoints() =>
        Children.SelectMany(c => c.GetPoints());
}
