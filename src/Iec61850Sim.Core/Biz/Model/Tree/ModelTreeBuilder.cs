using IEC61850.Server;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public class ModelTreeBuilder : IModelTreeBuilder
{
    public IModelNode Build(string iedName, IedModel model, IPointRegistry registry)
    {
        var iedNode = new IedNode(iedName);

        var ldNames = registry.All.Select(p => p.LogicalDevice).Distinct();

        foreach (var ldName in ldNames)
        {
            var ld = model.GetDeviceByInst(ldName);
            if (ld == null) continue;

            var ldNode = new LogicalDeviceNode(ldName);
            BuildLogicalNodes(ld, ldNode, registry);
            iedNode.AddChild(ldNode);
        }

        return iedNode;
    }

    private static void BuildLogicalNodes(LogicalDevice ld, LogicalDeviceNode ldNode, IPointRegistry registry)
    {
        var children = ld.GetChildren();
        if (children == null) return;

        foreach (ModelNode child in children)
        {
            if (child is not LogicalNode ln) continue;

            var lnNode = new LogicalNodeNode(ln.GetName());
            BuildDataObjects(ln, lnNode, registry);
            ldNode.AddChild(lnNode);
        }
    }

    private static void BuildDataObjects(ModelNode parent, ModelNodeBase parentNode, IPointRegistry registry)
    {
        var children = parent.GetChildren();
        if (children == null) return;

        foreach (ModelNode child in children)
        {
            if (child is not DataObject doObj) continue;

            var doNode = new DataObjectNode(doObj.GetName());

            BuildDataObjects(doObj, doNode, registry);  // recurse for SDOs
            AttachLeafPoints(doObj, doNode, registry);

            if (doNode.Children.Count > 0)
                parentNode.AddChild(doNode);
        }
    }

    private static void AttachLeafPoints(DataObject doObj, DataObjectNode doNode, IPointRegistry registry)
    {
        var seen = new HashSet<DevicePoint>(ReferenceEqualityComparer.Instance);
        CollectLeafPoints(doObj, doNode, registry, seen);
    }

    /// <summary>
    /// Recursively walks DataAttribute children (not DataObjects, which are handled by BuildDataObjects)
    /// to find leaf DAs whose full object reference matches a registry entry.
    /// This is required because value attributes (e.g. "f") are often nested inside
    /// structured DataAttributes (e.g. cVal → mag → f) rather than being direct children of the DO.
    /// </summary>
    private static void CollectLeafPoints(ModelNode node, DataObjectNode doNode, IPointRegistry registry, HashSet<DevicePoint> seen)
    {
        var children = node.GetChildren();
        if (children == null) return;

        foreach (ModelNode child in children)
        {
            if (child is DataObject) continue; // SDOs handled by BuildDataObjects
            if (child is not DataAttribute da) continue;

            if (da.GetChildren()?.Any() == true)
            {
                CollectLeafPoints(da, doNode, registry, seen);
            }
            else
            {
                var point = registry.GetPoint(da.GetObjectReference());
                if (point != null && seen.Add(point))
                    doNode.AddChild(new DataAttributeNode(point));
            }
        }
    }
}
