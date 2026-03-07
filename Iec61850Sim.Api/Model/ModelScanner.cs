using IEC61850.Server;
using Iec61850Sim.Api.Model;

namespace Iec61850Sim.Api.Model;


public class ModelScanner
{
    public void Scan(IedModel model, PointRegistry registry, IEnumerable<string> logicalDevices)
    {
        foreach (var ldName in logicalDevices)
        {
            var ld = model.GetDeviceByInst(ldName);

            if (ld == null)
                continue;

            ScanNode(ld, registry);
        }
    }

    private void ScanNode(ModelNode node, PointRegistry registry)
    {
        var children = node.GetChildren();

        if (children == null)
            return;

        foreach (ModelNode child in children)
        {
            if (child is DataAttribute da)
            {
                var point = new DevicePoint
                {
                    Reference = da.GetObjectReference(),
                    Attribute = da
                };

                registry.Register(point);
            }

            ScanNode(child, registry);
        }
    }
}