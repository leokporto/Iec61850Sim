using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Biz.Commands;
using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Model.Devices;

namespace Iec61850Sim.Core.Iec61850;

public class IecServerHost
{
    private readonly IedServer _server;
    private readonly PointRegistry _registry;
    private readonly ControlCommandProcessor _commandProcessor;
    private readonly DeviceManager _deviceManager;

    public IecServerHost(IedModel model, PointRegistry registry, DeviceManager deviceManager, ControlCommandProcessor commandProcessor)
    {
        _registry = registry;
        _deviceManager = deviceManager;
        _commandProcessor = commandProcessor;

        var config = new IedServerConfig();
        config.ReportBufferSize = 100000;

        _server = new IedServer(model, config);

        RegisterControlHandlers();

    }

    public IedServer Server => _server;

    public void Start(int port = 102)
    {
        _server.Start(port);

        if (_server.IsRunning())
            Console.WriteLine($"IEC61850 server started on port {port}");
        else
            Console.WriteLine("IEC61850 server failed to start");
    }

    public void Stop()
    {
        _server.Stop();
        _server.Destroy();
    }

    // Publica todos os pontos registrados — chamado pelo ciclo do BackgroundService
    public void Publish()
    {
        Publish(_registry.All);
    }

    // Publica apenas os pontos recebidos — chamado imediatamente após um comando
    public void Publish(IEnumerable<DevicePoint> points)
    {
        foreach (var point in points)
        {
            if (point.Value == null || point.ValueAttribute == null)
                continue;

            PublishValue(point);

            if (point.TimestampAttribute != null)
            {
                var ts = point.Timestamp.UtcDateTime == DateTime.MinValue
                    ? new Timestamp(DateTime.UtcNow)
                    : new Timestamp(point.Timestamp.DateTime);
                _server.UpdateTimestampAttributeValue(point.TimestampAttribute, ts);
            }

            if (point.QualityAttribute != null)
                _server.UpdateQuality(point.QualityAttribute, point.Quality);
        }
    }

    private void PublishValue(DevicePoint point)
    {
        switch (point.Value)
        {
            case float f:
                _server.UpdateFloatAttributeValue(point.ValueAttribute, f);
                break;

            case double d:
                _server.UpdateFloatAttributeValue(point.ValueAttribute, (float)d);
                break;

            case int i when point.ValueAttribute.Type == DataAttributeType.CODEDENUM:
                // Dbpos é MMS_BIT_STRING de 2 bits com encoding big-endian
                // Off=1 → [false,true], On=2 → [true,false]
                var dbpos = MmsValue.NewBitString(2);
                dbpos.BitStringFromUInt32BigEndian((uint)i);
                _server.UpdateAttributeValue(point.ValueAttribute, dbpos);
                break;

            case int i:
                _server.UpdateInt32AttributeValue(point.ValueAttribute, i);
                break;

            case bool b:
                _server.UpdateBooleanAttributeValue(point.ValueAttribute, b);
                break;
        }
    }

    private void RegisterControlHandlers()
    {
        var groups = _registry.GetByFc(FunctionalConstraint.CO)
            .Where(p => p.DataObject == "Pos")
            .GroupBy(p => p.LogicalNode);

        foreach (var g in groups)
        {
            var point = g.First();

            var pos = (DataObject)point.ValueAttribute
                .GetParent()   // Oper
                .GetParent();  // Pos

            var controller = _deviceManager.Controllers
                .FirstOrDefault(c => c.Name == g.Key);

            if (controller == null)
                continue;

            _server.SetCheckHandler(pos, CheckHandler, controller);
            _server.SetControlHandler(pos, ControlHandler, controller);
            _server.SetSelectStateChangedHandler(pos, SelectStateChanged, controller);
        }
    }

    private ControlHandlerResult ControlHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test)
    {
        var controller = (Cswi)parameter;

        _commandProcessor.Operate(controller, ctlVal, test);

        // Publica imediatamente o stVal do CSWI e do XCBR para o SCADA
        // receber o feedback antes (ou junto) ao CommandTermination
        Publish([controller.Position, controller.Breaker.Position]);

        return ControlHandlerResult.OK;
    }

    private CheckHandlerResult CheckHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test, bool interlockCheck)
    {
        return CheckHandlerResult.ACCEPTED;
    }

    private void SelectStateChanged(ControlAction action, object parameter, bool isSelected, SelectStateChangedReason reason)
    {
        // Apenas rastreia mudança de estado de seleção (SBO)
        // O comando é tratado exclusivamente no ControlHandler
        var controller = (Cswi)parameter;
        Console.WriteLine($"[CSWI] {controller.Name} select state: isSelected={isSelected}, reason={reason}");
    }
}

/*
 using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Biz.Commands;
using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Model.Devices;
using static System.Net.Mime.MediaTypeNames;


namespace Iec61850Sim.Core.Iec61850;

public class IecServerHost
{
    private readonly IedServer server;
    private readonly PointRegistry registry;
    private readonly ControlCommandProcessor commandProcessor;
    private readonly DeviceManager deviceManager;

    public IecServerHost(IedModel model, PointRegistry registry, DeviceManager deviceManager, ControlCommandProcessor commandProcessor)
    {
        this.registry = registry;
        this.deviceManager = deviceManager;
        this.commandProcessor = commandProcessor;

        var config = new IedServerConfig();
        config.ReportBufferSize = 100000;

        server = new IedServer(model, config);

        RegisterControlHandlers();
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
            if (point.Value == null || point.ValueAttribute == null)
                continue;

            switch (point.Value)
            {
                case float f:
                    server.UpdateFloatAttributeValue(point.ValueAttribute, f);
                    break;

                case double d:
                    server.UpdateFloatAttributeValue(point.ValueAttribute, (float)d);
                    break;

                case int i:
                    server.UpdateInt32AttributeValue(point.ValueAttribute, i);
                    break;

                case bool b:
                    server.UpdateBooleanAttributeValue(point.ValueAttribute, b);
                    break;
            }
            
            if(point.TimestampAttribute != null) {
                var timestamp = point.Timestamp.UtcDateTime == DateTime.MinValue ? new Timestamp(DateTime.UtcNow) : new Timestamp(point.Timestamp.DateTime);
                server.UpdateTimestampAttributeValue(point.TimestampAttribute, timestamp);
            }
                
            if (point.QualityAttribute != null) 
            {
                server.UpdateQuality(point.QualityAttribute, point.Quality);
            }


        }
    }

    private void RegisterControlHandlers()
    {
        var groups = registry.GetByFc(FunctionalConstraint.CO)
            .Where(p => p.DataObject == "Pos")
            .GroupBy(p => p.LogicalNode);

        foreach (var g in groups)
        {
            var point = g.First();

            var pos = (DataObject)point.ValueAttribute
                .GetParent()   // Oper
                .GetParent();  // Pos

            var controller = deviceManager.Controllers
                .FirstOrDefault(c => c.Name == g.Key);

            if (controller == null)
                continue;

            server.SetCheckHandler(pos, CheckHandler, controller);
            server.SetControlHandler(pos, ControlHandler, controller);
            server.SetSelectStateChangedHandler(pos, SelectStateChanged, controller);
        }
    }

    private ControlHandlerResult ControlHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test)
    {
        var controller = (Cswi)parameter;

        commandProcessor.Operate(controller, ctlVal, test);

        

        return ControlHandlerResult.OK;
       
    }

    private CheckHandlerResult CheckHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test, bool interlockCheck)
    {
        return CheckHandlerResult.ACCEPTED;
    }

    private void SelectStateChanged(ControlAction action, object parameter, bool isSelected, SelectStateChangedReason reason)
    {
        DataObject cObj = action.GetControlObject();

        var controller = (Cswi)parameter;

        commandProcessor.Operate(controller, isSelected);

        Publish();
    }

    
}
*/