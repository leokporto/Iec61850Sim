namespace Iec61850Sim.Core.Model.Tree;

public sealed class LogicalDeviceNode : ModelNodeBase
{
    public LogicalDeviceNode(string name) : base(name) { }

    public override IEnumerable<PointValue<object>> GetPoints() => [];
}