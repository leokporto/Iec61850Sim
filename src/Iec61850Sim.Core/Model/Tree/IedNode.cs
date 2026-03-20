namespace Iec61850Sim.Core.Model.Tree;

public sealed class IedNode : ModelNodeBase
{
    public IedNode(string name) : base(name) { }

    public override IEnumerable<PointValue<object>> GetPoints() => [];
}