using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

/// <summary>
/// Disconnector / earthing switch (XSWI in IEC 61850-7-4).
/// Unlike XCBR, XSWI simulates a physical maneuver time: on Open/Close the position is set to
/// Intermediate (0) and transitions to the final state after <see cref="ManeuverTime"/> seconds.
/// If travel exceeds <see cref="SwAbmTime"/>, the SwAbm abnormal alarm is raised.
/// </summary>
public class XSWI : DeviceBase, ISwitchingDevice
{
    private static readonly Dictionary<string, object> Defaults = new()
    {
        ["Loc.stVal"] = false,
        ["BlkOpn.stVal"] = false,
        ["BlkCls.stVal"] = false,
        ["SwAbm.stVal"] = false
    };

    private readonly IPointRegistry _registry;
    private readonly string? _blkOpnRef;
    private readonly string? _blkClsRef;
    private readonly string? _swAbmRef;

    // ── Maneuver state machine ─────────────────────────────────────────────
    private bool _traveling;
    private int _targetPosition;       // 1 = Open, 2 = Close
    private double _travelElapsed;     // seconds since maneuver started

    public string PositionReference { get; }
    public string EquipmentName { get; }
    public string? LocReference { get; }

    /// <summary>Simulated physical travel time in seconds (default 2 s).</summary>
    public double ManeuverTime { get; }

    /// <summary>
    /// Seconds after which <c>SwAbm.stVal</c> is set to true when the switch is stuck
    /// in the Intermediate state (default 30 s).
    /// </summary>
    public double SwAbmTime { get; }

    public XSWI(
        string name,
        string positionReference,
        string equipmentName,
        IPointRegistry registry,
        string? locRef = null,
        string? blkOpnRef = null,
        string? blkClsRef = null,
        string? swAbmRef = null,
        double maneuverTime = 2.0,
        double swAbmTime = 30.0)
        : base(name)
    {
        PositionReference = positionReference;
        EquipmentName = equipmentName;
        _registry = registry;
        LocReference = locRef;
        _blkOpnRef = blkOpnRef;
        _blkClsRef = blkClsRef;
        _swAbmRef = swAbmRef;
        ManeuverTime = maneuverTime;
        SwAbmTime = swAbmTime;
    }

    /// <summary>
    /// Reads current model values via <paramref name="readAttributeValue"/> and seeds the registry
    /// with defaults for any type-default values found in the model.
    /// </summary>
    public void Initialize(Func<DataAttribute, object?> readAttributeValue)
    {
        var optional = new[] { LocReference, _blkOpnRef, _blkClsRef, _swAbmRef };
        foreach (var pointRef in optional.Where(r => r != null))
        {
            var point = _registry.GetPoint(pointRef!);
            if (point?.ValueAttribute == null)
                continue;

            var cfgValue = readAttributeValue(point.ValueAttribute);
            var key = $"{point.DataObject}.{point.ValueAttribute.GetName()}";

            var effectiveValue = IsTypeDefault(cfgValue)
                ? Defaults.GetValueOrDefault(key)
                : cfgValue;

            if (effectiveValue == null)
                continue;

            _registry.SetValue(new PointValue<object>
            {
                Reference = pointRef!,
                Value = effectiveValue,
                Quality = 192,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    // ── ISwitchingDevice ──────────────────────────────────────────────────

    /// <summary>
    /// Initiates opening. Sets position to Intermediate (0) and starts the maneuver timer.
    /// Returns null on acceptance; the final position is applied in <see cref="Step"/>.
    /// Returns a blocking cause without changing position when Loc or BlkOpn prevents the operation.
    /// </summary>
    public ControlAddCause? Open()
    {
        var blockCause = CheckPreConditions(isOpen: true);
        if (blockCause != null)
            return blockCause;

        StartTravel(targetPosition: 1);
        return null;
    }

    /// <summary>
    /// Initiates closing. Sets position to Intermediate (0) and starts the maneuver timer.
    /// Returns null on acceptance; the final position is applied in <see cref="Step"/>.
    /// Returns a blocking cause without changing position when Loc or BlkCls prevents the operation.
    /// </summary>
    public ControlAddCause? Close()
    {
        var blockCause = CheckPreConditions(isOpen: false);
        if (blockCause != null)
            return blockCause;

        StartTravel(targetPosition: 2);
        return null;
    }

    // ── Simulation loop ───────────────────────────────────────────────────

    public override void Step(double dt)
    {
        if (!_traveling)
            return;

        _travelElapsed += dt;

        if (_travelElapsed >= ManeuverTime)
        {
            // Maneuver complete — apply final position
            _registry.SetValue(new PointValue<object>
            {
                Reference = PositionReference,
                Value = _targetPosition,
                Quality = 192,
                Timestamp = DateTimeOffset.UtcNow
            });

            ClearSwAbm();
            _traveling = false;
            _travelElapsed = 0;
            return;
        }

        // Raise SwAbm if switch has been intermediate for too long
        if (_swAbmRef != null && _travelElapsed >= SwAbmTime)
            SetSwAbm(true);
    }

    // ── Internals ─────────────────────────────────────────────────────────

    private ControlAddCause? CheckPreConditions(bool isOpen)
    {
        if (LocReference != null && _registry.GetValue<object>(LocReference).Value is true)
            return ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY;

        if (isOpen && _blkOpnRef != null && _registry.GetValue<object>(_blkOpnRef).Value is true)
            return ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING;

        if (!isOpen && _blkClsRef != null && _registry.GetValue<object>(_blkClsRef).Value is true)
            return ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING;

        return null;
    }

    private void StartTravel(int targetPosition)
    {
        _targetPosition = targetPosition;
        _traveling = true;
        _travelElapsed = 0;

        // Immediately set Intermediate to signal ongoing maneuver
        _registry.SetValue(new PointValue<object>
        {
            Reference = PositionReference,
            Value = (int)eDblPos.Intermediate,
            Quality = 192,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private void ClearSwAbm()
    {
        if (_swAbmRef == null)
            return;

        var current = _registry.GetValue<object>(_swAbmRef).Value;
        if (current is true)
            SetSwAbm(false);
    }

    private void SetSwAbm(bool value)
    {
        _registry.SetValue(new PointValue<object>
        {
            Reference = _swAbmRef!,
            Value = value,
            Quality = 192,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private static bool IsTypeDefault(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrEmpty(s),
        int i => i == 0,
        bool b => !b,
        _ => true
    };
}
