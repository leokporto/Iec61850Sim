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

    // Armazena o ctlVal do SelectWithValue (SBO) — capturado no CheckHandler,
    // consumido no SelectStateChanged após confirmação do SELECT.
    private readonly Dictionary<string, MmsValue> _pendingSelectCtlVals = new();

    public IecServerHost(IedModel model, PointRegistry registry, DeviceManager deviceManager, ControlCommandProcessor commandProcessor)
    {
        _registry = registry;
        _deviceManager = deviceManager;
        _commandProcessor = commandProcessor;

        var config = new IedServerConfig();
        config.ReportBufferSize = 100000;

        _server = new IedServer(model, config);

        _server.SetServerIdentity("IEC61850-Sim", "Demo", "1.0.0");

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

    // Publica pontos por referência — usado nos handlers de comando
    private void PublishByReferences(IEnumerable<string> references)
    {
        var points = references
            .Select(r => _registry.GetPoint(r))
            .Where(p => p != null)
            .Cast<DevicePoint>();

        Publish(points);
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
        PublishByReferences([controller.PositionReference, controller.Breaker.PositionReference]);

        return ControlHandlerResult.OK;
    }

    private CheckHandlerResult CheckHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test, bool interlockCheck)
    {
        // Para SBO (ctlModel=4): o ctlVal não é acessível via ControlAction no SelectStateChanged,
        // então é capturado aqui.
        if (ctlVal != null && parameter is Cswi ctrl)
            _pendingSelectCtlVals[ctrl.Name] = ctlVal;

        return CheckHandlerResult.ACCEPTED;
    }

    private void SelectStateChanged(ControlAction action, object parameter, bool isSelected, SelectStateChangedReason reason)
    {
        var controller = (Cswi)parameter;
        Console.WriteLine($"[CSWI] {controller.Name} select state: isSelected={isSelected}, reason={reason}");

        if (!isSelected || reason != SelectStateChangedReason.SELECT_STATE_REASON_SELECTED)
        {
            _pendingSelectCtlVals.Remove(controller.Name);
            return;
        }

        if (!_pendingSelectCtlVals.TryGetValue(controller.Name, out MmsValue? ctlVal))
            return;

        // SBO (ctlModel=4): aplica o comando com o ctlVal do SelectWithValue
        _commandProcessor.Operate(controller, ctlVal, test: false);
        PublishByReferences([controller.PositionReference, controller.Breaker.PositionReference]);
    }
}
