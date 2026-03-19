using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public sealed class LogicalDeviceNode : ModelNodeBase
{
    public LogicalDeviceNode(string name) : base(name) { }

    public override IEnumerable<DevicePoint> GetPoints() => [];
}
