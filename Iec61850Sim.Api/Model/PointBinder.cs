using IEC61850.Server;
using Iec61850Sim.Api.Model.Devices;

namespace Iec61850Sim.Api.Model;

public class PointBinder
{
    public void Bind(
        IedModel model,
        DeviceManager deviceManager,
        IedServer server)
    {
        foreach (var breaker in deviceManager.Breakers)
        {
            var controlRef = $"DemoProtCtrl/{breaker.Name}.Pos";

            var node =
                (DataObject)model.GetModelNodeByShortObjectReference(controlRef);

            if (node == null)
                continue;

            Console.WriteLine($"Binding control handler: {controlRef}");

            server.SetControlHandler(
                node,
                (action, parameter, ctlVal, test) =>
                {
                    bool close = ctlVal.GetBoolean();

                    if (close)
                        breaker.Close();
                    else
                        breaker.Open();

                    Console.WriteLine(
                        $"Breaker {breaker.Name} command: {(close ? "CLOSE" : "OPEN")}");

                    return ControlHandlerResult.OK;
                },
                null);
        }
    }
}