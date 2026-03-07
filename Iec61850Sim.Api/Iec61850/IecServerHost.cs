using IEC61850.Server;
using Iec61850Sim.Api.Core;
using Iec61850Sim.Api.Core.Model;

namespace Iec61850Sim.Api.Iec61850;

public class IecServerHost
{
    private readonly IedServer server;
    private readonly PointRegistry registry;

    public IecServerHost(IedServer server, PointRegistry registry)
    {
        this.server = server;
        this.registry = registry;
    }

    public void Publish()
    {
        foreach (var point in registry.All)
        {
            if (point.Value is float f)
                server.UpdateFloatAttributeValue(point.Attribute, f);

            if (point.Value is int i)
                server.UpdateInt32AttributeValue(point.Attribute, i);
        }
    }
}