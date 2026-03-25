using System.Collections.Concurrent;
using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Commands;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Iec61850;

public class IecServerHost
{
    private readonly IedServer _server;
    private readonly PointRegistry _registry;
    private readonly ControlCommandProcessor _commandProcessor;
    private readonly DeviceManager _deviceManager;

    // Task 7: ConcurrentDictionary for thread-safe access from libiec61850 callbacks.
    // Stores the ctlVal from SelectWithValue (SBO) — captured in CheckHandler so it is
    // available when ControlHandler fires on the subsequent Operate.
    private readonly ConcurrentDictionary<string, MmsValue> _pendingSelectCtlVals = new();

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

            case string s:
                _server.UpdateVisibleStringAttributeValue(point.ValueAttribute, s);
                break;
        }
    }

    /// <summary>
    /// Reads the current value of a DataAttribute from the IEC server model.
    /// Returns null when the attribute type is not handled or the value is unavailable.
    /// </summary>
    public object? ReadAttributeValue(DataAttribute da)
    {
        var mmsValue = _server.GetAttributeValue(da);
        if (mmsValue == null)
            return null;

        var mmsType = mmsValue.GetType();

        if (mmsType == MmsType.MMS_VISIBLE_STRING)
        {
            var s = mmsValue.ToString();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        if (mmsType == MmsType.MMS_INTEGER)
            return mmsValue.ToInt32();

        if (mmsType == MmsType.MMS_BOOLEAN)
            return mmsValue.GetBoolean();

        return null;
    }

    // Task 6: Read ctlModel and register only the handlers appropriate for that model.
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

            var ctlModel = ReadCtlModel(pos);

            _server.SetCheckHandler(pos, CheckHandler, controller);
            _server.SetControlHandler(pos, ControlHandler, controller);

            if (ctlModel is ControlModel.SBO_NORMAL or ControlModel.SBO_ENHANCED)
                _server.SetSelectStateChangedHandler(pos, SelectStateChanged, controller);
        }
    }

    // Task 6: Reads the ctlModel attribute from the Pos DataObject's children.
    // Defaults to DIRECT_NORMAL when the attribute is absent or unreadable.
    private ControlModel ReadCtlModel(DataObject pos)
    {
        try
        {
            var children = pos.GetChildren();
            if (children == null) return ControlModel.DIRECT_NORMAL;

            foreach (ModelNode child in children)
            {
                if (child is DataAttribute da && da.GetName() == "ctlModel")
                {
                    var val = _server.GetAttributeValue(da);
                    if (val != null && val.GetType() == MmsType.MMS_INTEGER)
                        return (ControlModel)val.ToInt32();
                }
            }
        }
        catch { }

        return ControlModel.DIRECT_NORMAL;
    }

    // Task 2: Map ControlOperateResult to ControlHandlerResult.
    // On failure, SetAddCause so the library includes it in CommandTermination-.
    private ControlHandlerResult ControlHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test)
    {
        var controller = (CSWI)parameter;
        var result = _commandProcessor.Operate(controller, ctlVal, test);

        if (!result.Success)
        {
            TrySetAddCause(action, result.Cause);
            return ControlHandlerResult.FAILED;
        }

        // Publish stVal immediately so the SCADA receives feedback with CommandTermination+
        PublishByReferences([controller.PositionReference, controller.SwitchDevice.PositionReference]);
        return ControlHandlerResult.OK;
    }

    // Task 4: Validate before the command reaches ControlHandler.
    // Failures return HARDWARE_FAULT so the library generates a Response- with AddCause.
    private CheckHandlerResult CheckHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test, bool interlockCheck)
    {
        // Store ctlVal for SBO models — ControlAction does not expose it in ControlHandler for SBO.
        if (ctlVal != null && parameter is CSWI ctrl)
        {
            _pendingSelectCtlVals[ctrl.Name] = ctlVal;

            // Decode command direction for interlock checks
            var pos = ctlVal.GetType() == MmsType.MMS_BOOLEAN
                ? (ctlVal.GetBoolean() ? eDblPos.On : eDblPos.Off)
                : (eDblPos)ctlVal.ToInt32();

            // 1. Local mode — reject remote MMS commands
            if (ctrl.LocReference != null && _registry.GetValue<object>(ctrl.LocReference).Value is true)
            {
                TrySetAddCause(action, ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY);
                return CheckHandlerResult.HARDWARE_FAULT;
            }

            // 2. CILO interlocking (only when the client requests interlock check)
            if (interlockCheck && ctrl.Cilo != null)
            {
                if (pos == eDblPos.Off && !ctrl.Cilo.CanOpen(_registry))
                {
                    TrySetAddCause(action, ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING);
                    return CheckHandlerResult.HARDWARE_FAULT;
                }
                if (pos == eDblPos.On && !ctrl.Cilo.CanClose(_registry))
                {
                    TrySetAddCause(action, ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING);
                    return CheckHandlerResult.HARDWARE_FAULT;
                }
            }

            // 3. Mode — reject when governing LLN0 is not in On (1) mode
            var ld = ctrl.PositionReference.Split('/')[0];
            var lln0 = _deviceManager.LLN0Devices.FirstOrDefault(l => l.Name == $"LLN0_{ld}");
            if (lln0?.ModReference != null)
            {
                var mod = _registry.GetValue<object>(lln0.ModReference).Value;
                if (mod is int modInt && modInt != 1)
                {
                    TrySetAddCause(action, ControlAddCause.ADD_CAUSE_BLOCKED_BY_MODE);
                    return CheckHandlerResult.HARDWARE_FAULT;
                }
            }
        }

        return CheckHandlerResult.ACCEPTED;
    }

    // Task 1: SelectStateChanged must never execute a command.
    // Execution happens exclusively in ControlHandler when the client sends Operate.
    private void SelectStateChanged(ControlAction action, object parameter, bool isSelected, SelectStateChangedReason reason)
    {
        var controller = (CSWI)parameter;
        Console.WriteLine($"[CSWI] {controller.Name} select state: isSelected={isSelected}, reason={reason}");

        // Clean up pending ctlVal on any deselection event.
        // On SELECTED: retain pending ctlVal; ControlHandler will consume it on Operate.
        if (!isSelected || reason != SelectStateChangedReason.SELECT_STATE_REASON_SELECTED)
            _pendingSelectCtlVals.TryRemove(controller.Name, out _);
    }

    // Wraps ControlAction.SetAddCause to be safe when called from test context with an
    // uninitialized ControlAction (no native handle). In production the action is always valid.
    private static void TrySetAddCause(ControlAction action, ControlAddCause cause)
    {
        try { action.SetAddCause(cause); }
        catch { }
    }
}
