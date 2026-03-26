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
    private IedServer _server;
    private readonly PointRegistry _registry;
    private readonly ControlCommandProcessor _commandProcessor;
    private readonly DeviceManager _deviceManager;
    private int _connectedClients;
    private int _port = 102;
    private readonly string _model = "Demo";

    // Task 1: RCB handle cache — populated at startup and lazily from callbacks.
    private RcbManager _rcbManager = new();

    // Thread-safe connected client counter — incremented/decremented in the connection callback.
    public int ConnectedClients => _connectedClients;

    public int Port => _port;

    public string ServerModel => _model;


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

        // Task 1: resolve and cache RCB handles before the server starts.
        _rcbManager = new RcbManager();
        _rcbManager.Initialize(model, _registry);

        RegisterControlHandlers();
        RegisterRcbEventHandler();
        RegisterConnectionHandler();
    }

    public IedServer Server => _server;

    public void Start(int port = 102)
    {
        _port = port;
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

    /// <summary>
    /// Recreates the internal IedServer with a new model. Does NOT start the server.
    /// Call RuntimeEngine.StartServer(port) afterward to start on the desired port.
    /// </summary>
    public void Reinitialize(IedModel model, string serverModel)
    {
        _pendingSelectCtlVals.Clear();

        var config = new IedServerConfig();
        config.ReportBufferSize = 100000;

        _server = new IedServer(model, config);
        _server.SetServerIdentity("IEC61850-Sim", serverModel, "1.0.0");

        // Task 1: re-initialise handle cache for the new model.
        _rcbManager = new RcbManager();
        _rcbManager.Initialize(model, _registry);

        _connectedClients = 0;

        RegisterControlHandlers();
        RegisterRcbEventHandler();
        RegisterConnectionHandler();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 3 — Report Trigger Logic (TrgOps)
    //
    // libiec61850 automatically evaluates TrgOps on every attribute update:
    //
    //   • dchg  — fires when UpdateXxxAttributeValue detects a value change.
    //   • qchg  — fires when UpdateQuality detects a quality change.
    //   • intg  — fires independently per-RCB on the IntgPd timer maintained
    //             by the library; no application action is needed.
    //
    // The simulation loop calls Publish() every 1–2 s.  Control handlers call
    // Publish(selectedPoints) immediately after command execution so the SCADA
    // receives feedback synchronously in the same MMS exchange.
    //
    // Examples:
    //   Analog measurement  → UpdateFloatAttributeValue(MMXU1.TotW.mag.f, value)
    //   Breaker status      → UpdateAttributeValue(XCBR1.Pos.stVal, dbposBitString)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Publishes all registered points — called by the BackgroundService simulation loop.</summary>
    public void Publish()
    {
        Publish(_registry.All);
    }

    /// <summary>
    /// Publishes the supplied points into the IedModel.
    /// Each UpdateXxxAttributeValue call triggers TrgOps evaluation (dchg/qchg) for any
    /// enabled RCB whose DataSet references the updated attribute.
    /// </summary>
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

    // Publishes points by reference — used inside command handlers.
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
                // Dbpos is an MMS BIT_STRING(2) with big-endian encoding.
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

    // ─────────────────────────────────────────────────────────────────────────
    // Task 2 — RCB Event Handler
    // ─────────────────────────────────────────────────────────────────────────

    private void RegisterRcbEventHandler()
    {
        _server.SetRCBEventHandler(OnRcbEvent, null);
    }

    /// <summary>
    /// Global callback fired by libiec61850 for all RCB state changes.
    ///
    /// ENABLED        — client set RptEna=true; spontaneous reporting is now active.
    /// DISABLED       — client set RptEna=false.
    /// SET_PARAMETER  — client wrote an RCB attribute.  Writes to critical attributes
    ///                  while RptEna=true are logged; the library enforces the constraint
    ///                  natively for mandatory-disabled parameters (e.g. DatSet on BRCB).
    /// GI             — client requested a General Interrogation (see Task 5).
    /// PURGEBUF       — BRCB buffer was purged; sequence numbering resets (Task 4).
    /// OVERFLOW       — BRCB buffer overflowed; entry loss should be alarmed.
    /// REPORT_CREATED — a report entry was inserted into the BRCB buffer (normal operation).
    /// </summary>
    private void OnRcbEvent(object? parameter, ReportControlBlock rcb,
        ClientConnection con, RCBEventType eventType, string parameterName,
        MmsDataAccessError serviceError)
    {
        // Task 1: ensure the cache is populated even if startup traversal missed this RCB.
        _rcbManager.EnsureCached(rcb);

        switch (eventType)
        {
            case RCBEventType.ENABLED:
                Console.WriteLine($"[RCB] {rcb.Name} ENABLED  DataSet='{rcb.DataSet}'");
                break;

            case RCBEventType.DISABLED:
                Console.WriteLine($"[RCB] {rcb.Name} DISABLED");
                break;

            // Task 2: detect attempted reconfiguration of critical attributes while enabled.
            // libiec61850 v1.6 fires SET_PARAMETER before applying the write.  In the C API
            // the handler can set the serviceError out-param to block the write; the .NET
            // delegate receives serviceError by value, so the block relies on the library's
            // built-in constraints for attributes that require RptEna=false (e.g. DatSet on
            // a BRCB).  We log the attempt for all critical attributes as an audit trail.
            case RCBEventType.SET_PARAMETER:
                if (rcb.RptEna && IsCriticalRcbAttribute(parameterName))
                {
                    Console.WriteLine(
                        $"[RCB] WARNING: '{parameterName}' written on '{rcb.Name}' " +
                        $"while RptEna=true — library constraint applies.");
                }
                break;

            // Task 5: General Interrogation
            case RCBEventType.GI:
                HandleGi(rcb);
                break;

            // Task 4: BRCB buffer lifecycle
            case RCBEventType.PURGEBUF:
                // Buffer purged: sequence numbers reset.  Occurs on client reconnect if the
                // client did not present a valid EntryID, or on explicit client request.
                // The library maintains buffer state; no application-level action is needed.
                Console.WriteLine($"[BRCB] {rcb.Name} buffer PURGED — SqNum reset");
                break;

            case RCBEventType.OVERFLOW:
                // ReportBufferSize exceeded.  Consider increasing IedServerConfig.ReportBufferSize
                // or reducing the simulation publish rate for high-frequency datasets.
                Console.WriteLine($"[BRCB] {rcb.Name} buffer OVERFLOW — some events may be lost");
                break;

            case RCBEventType.REPORT_CREATED:
                // Normal BRCB operation: an entry was buffered.  Log only at trace level.
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 5 — General Interrogation (GI) Response
    //
    // Flow:
    //   1. OnRcbEvent receives RCBEventType.GI before the library assembles the report.
    //   2. HandleGi identifies the DataSet owner LD and calls Publish() to flush the
    //      current simulation state into the IedModel for that scope.
    //   3. The library reads the freshly updated IedModel values and sends one MMS report
    //      containing the full DataSet snapshot to the requesting client.
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleGi(ReportControlBlock rcb)
    {
        // Scope the refresh to the RCB's owning LD so we avoid unnecessary writes.
        // For cross-LD DataSets (GetDataSetLd returns null) we fall back to all points.
        var ldName = _rcbManager.GetDataSetLd(rcb);

        var points = ldName != null
            ? _registry.All.Where(p => p.LogicalDevice == ldName)
            : _registry.All;

        Publish(points);

        Console.WriteLine($"[GI] {rcb.Name} → snapshot refreshed (scope: {ldName ?? "ALL"})");
    }

    // Critical RCB attributes: changing these while RptEna=true is non-compliant per IEC 61850.
    private static bool IsCriticalRcbAttribute(string? parameterName) =>
        parameterName is "DatSet" or "TrgOps" or "OptFlds" or "IntgPd" or "BufTm" or "ConfRev";

    private void RegisterConnectionHandler()
    {
        _server.SetConnectionIndicationHandler(OnConnectionIndication, null);
    }

    private void OnConnectionIndication(IedServer server, ClientConnection connection,
        bool connected, object? parameter)
    {
        if (connected)
            Interlocked.Increment(ref _connectedClients);
        else
            Interlocked.Decrement(ref _connectedClients);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Task 4 — BRCB Buffering Notes
    //
    // The libiec61850 library manages the BRCB buffer automatically:
    //
    //   • On RptEna=true (ENABLED event):
    //       - If the client presents a valid EntryID (resume), the library delivers
    //         buffered entries starting from that sequence number (SoE delivery).
    //       - If no EntryID or EntryID is invalid, PurgeBuf fires and the buffer is
    //         cleared before new entries are accepted.
    //
    //   • During connection drop:
    //       - The buffer continues accepting entries (up to ReportBufferSize).
    //       - SqNum increments with each buffered report.
    //       - TimeofEntry records the exact time each entry was buffered.
    //
    //   • On reconnect and RptEna=true with a valid EntryID:
    //       - The library replays all buffered entries in sequence order (SoE).
    //       - Once the buffer is drained, live reporting resumes automatically.
    //
    //   • OVERFLOW event means ReportBufferSize was exhausted; increase the buffer
    //     in IedServerConfig or reduce the event rate for that DataSet.
    //
    // No application-level buffer management is required beyond the IedServerConfig
    // setting and responding to OVERFLOW (alert + optionally pause high-freq updates).
    // ─────────────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────────
    // Task 6 (existing): Control handler registration
    // ─────────────────────────────────────────────────────────────────────────

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

    private CheckHandlerResult CheckHandler(ControlAction action, object parameter, MmsValue ctlVal, bool test, bool interlockCheck)
    {
        if (ctlVal != null && parameter is CSWI ctrl)
        {
            _pendingSelectCtlVals[ctrl.Name] = ctlVal;

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

    private void SelectStateChanged(ControlAction action, object parameter, bool isSelected, SelectStateChangedReason reason)
    {
        var controller = (CSWI)parameter;
        Console.WriteLine($"[CSWI] {controller.Name} select state: isSelected={isSelected}, reason={reason}");

        if (!isSelected || reason != SelectStateChangedReason.SELECT_STATE_REASON_SELECTED)
            _pendingSelectCtlVals.TryRemove(controller.Name, out _);
    }

    private static void TrySetAddCause(ControlAction action, ControlAddCause cause)
    {
        try { action.SetAddCause(cause); }
        catch { }
    }
}
