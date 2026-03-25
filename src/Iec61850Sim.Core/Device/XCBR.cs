using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

public class XCBR : DeviceBase
{
    private static readonly Dictionary<string, object> Defaults = new()
    {
        ["Loc.stVal"] = false,
        ["BlkOpn.stVal"] = false,
        ["BlkCls.stVal"] = false,
        ["OpCnt.stVal"] = 0,
        ["CBOpCap.stVal"] = 4
    };

    private readonly IPointRegistry _registry;
    private readonly string? _locRef;
    private readonly string? _blkOpnRef;
    private readonly string? _blkClsRef;
    private readonly string? _opCntRef;
    private readonly string? _cbOpCapRef;

    public string PositionReference { get; }
    public string EquipmentName { get; }
    public string? LocReference => _locRef;

    public XCBR(
        string name,
        string positionReference,
        string equipmentName,
        IPointRegistry registry,
        string? locRef = null,
        string? blkOpnRef = null,
        string? blkClsRef = null,
        string? opCntRef = null,
        string? cbOpCapRef = null)
        : base(name)
    {
        PositionReference = positionReference;
        EquipmentName = equipmentName;
        _registry = registry;
        _locRef = locRef;
        _blkOpnRef = blkOpnRef;
        _blkClsRef = blkClsRef;
        _opCntRef = opCntRef;
        _cbOpCapRef = cbOpCapRef;
    }

    /// <summary>
    /// Reads current model values via <paramref name="readAttributeValue"/> and initializes
    /// mandatory DOs in the registry. Applies hardcoded defaults when the model value is a type default.
    /// </summary>
    public void Initialize(Func<DataAttribute, object?> readAttributeValue)
    {
        var mandatoryRefs = new[] { _locRef, _blkOpnRef, _blkClsRef, _opCntRef, _cbOpCapRef }
            .Where(r => r != null);

        foreach (var pointRef in mandatoryRefs)
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

    /// <summary>
    /// Opens the breaker. Returns null on success, or the blocking <see cref="ControlAddCause"/> on failure.
    /// </summary>
    public ControlAddCause? Open()
    {
        if (_locRef != null && _registry.GetValue<object>(_locRef).Value is true)
            return ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY;

        if (_blkOpnRef != null && _registry.GetValue<object>(_blkOpnRef).Value is true)
            return ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING;

        _registry.SetValue(new PointValue<object>
        {
            Reference = PositionReference,
            Value = 1,
            Timestamp = DateTimeOffset.UtcNow,
            Quality = 192
        });

        IncrementOpCnt();
        return null;
    }

    /// <summary>
    /// Closes the breaker. Returns null on success, or the blocking <see cref="ControlAddCause"/> on failure.
    /// </summary>
    public ControlAddCause? Close()
    {
        if (_locRef != null && _registry.GetValue<object>(_locRef).Value is true)
            return ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY;

        if (_blkClsRef != null && _registry.GetValue<object>(_blkClsRef).Value is true)
            return ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING;

        _registry.SetValue(new PointValue<object>
        {
            Reference = PositionReference,
            Value = 2,
            Timestamp = DateTimeOffset.UtcNow,
            Quality = 192
        });

        IncrementOpCnt();
        return null;
    }

    public override void Step(double dt) { }

    private void IncrementOpCnt()
    {
        if (_opCntRef == null)
            return;

        var current = _registry.GetValue<object>(_opCntRef).Value;
        var count = current is int i ? i : 0;

        _registry.SetValue(new PointValue<object>
        {
            Reference = _opCntRef,
            Value = count + 1,
            Quality = 192,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private static bool IsTypeDefault(object? value)
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrEmpty(s),
            int i => i == 0,
            short s => s == 0,
            bool b => !b,
            _ => true
        };
    }
}
