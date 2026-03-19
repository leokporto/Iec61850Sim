using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public abstract class ModelNodeBase : IModelNode
{
    private readonly List<IModelNode> _children = new();

    public string Name { get; }
    public IReadOnlyList<IModelNode> Children => _children;

    protected ModelNodeBase(string name)
    {
        Name = name;
    }

    internal void AddChild(IModelNode child) => _children.Add(child);

    public abstract IEnumerable<DevicePoint> GetPoints();
}
