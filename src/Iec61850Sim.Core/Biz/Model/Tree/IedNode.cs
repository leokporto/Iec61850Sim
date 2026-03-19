using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public sealed class IedNode : ModelNodeBase
{
    public IedNode(string name) : base(name) { }

    public override IEnumerable<PointValue<object>> GetPoints() => [];
}
