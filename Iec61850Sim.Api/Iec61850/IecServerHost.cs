using IEC61850.Server;
using Iec61850Sim.Api.Core;
using Iec61850Sim.Api.Model;

namespace Iec61850Sim.Api.Iec61850;

public class IecServerHost
{
    private readonly IedServer server;
    private readonly PointRegistry registry;

    public IecServerHost(IedModel model, PointRegistry registry)
    {
        this.registry = registry;

        var config = new IedServerConfig();
        config.ReportBufferSize = 100000;

        server = new IedServer(model, config);
    }
    
    public IedServer Server => server;

    public void Start(int port = 102)
    {
        server.Start(port);

        if (server.IsRunning())
            Console.WriteLine($"IEC61850 server started on port {port}");
        else
            Console.WriteLine("IEC61850 server failed to start");
    }

    public void Stop()
    {
        server.Stop();
        server.Destroy();
    }

    public void Publish()
    {
        foreach (var point in registry.All)
        {
            if (point.Value == null)
                continue;

            switch (point.Value)
            {
                case float f:
                    server.UpdateFloatAttributeValue(point.Attribute, f);
                    break;

                case double d:
                    server.UpdateFloatAttributeValue(point.Attribute, (float)d);
                    break;

                case int i:
                    server.UpdateInt32AttributeValue(point.Attribute, i);
                    break;

                case bool b:
                    server.UpdateBooleanAttributeValue(point.Attribute, b);
                    break;
            }
        }
    }
}