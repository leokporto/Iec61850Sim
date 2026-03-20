namespace Iec61850Sim.Core.Model.Tree;

public sealed class DataObjectNode : ModelNodeBase
{
    public DataObjectNode(string name) : base(name) { }

    public override IEnumerable<PointValue<object>> GetPoints() =>
        Children.SelectMany(c => c.GetPoints());
}