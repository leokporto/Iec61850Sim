using IEC61850.Server;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

/// <summary>
/// Switch controller (CSWI on IEC 61850).
/// Validates and delegates physical switching to the associated breaker (XCBR).
/// Optional points (Loc, OpCntRs, OpOpn, OpCls) only participate in logic when present in the model.
/// </summary>
public class CSWI : DeviceBase
{
    private static readonly Dictionary<string, object> Defaults = new()
    {
        ["Loc.stVal"] = false,
        ["OpCntRs.stVal"] = 0
    };

    private readonly IPointRegistry _registry;

    public CSWI(
        string name,
        string commandReference,
        string positionReference,
        ISwitchingDevice switchDevice,
        IPointRegistry registry,
        string? locRef = null,
        string? opCntRsRef = null,
        string? opOpnRef = null,
        string? opClsRef = null)
        : base(name)
    {
        CommandReference = commandReference;
        PositionReference = positionReference;
        SwitchDevice = switchDevice;
        _registry = registry;
        LocReference = locRef;
        OpCntRsReference = opCntRsRef;
        OpOpnReference = opOpnRef;
        OpClsReference = opClsRef;
    }

    /// <summary>The physical switching device (XCBR or XSWI) controlled by this CSWI.</summary>
    public ISwitchingDevice SwitchDevice { get; private set; }

    /// <summary>Convenience accessor when the underlying device is an XCBR; null otherwise.</summary>
    public XCBR? Xcbr => SwitchDevice as XCBR;

    /// <summary>Optional interlock (CILO) linked to this CSWI by equipment name. Null when not in model.</summary>
    public CILO? Cilo { get; private set; }

    /// <summary>Reference of the CO point used to receive commands (CSWI.Pos.Oper.ctlVal).</summary>
    public string CommandReference { get; }

    /// <summary>Reference of the ST point mirroring the breaker position (CSWI.Pos.stVal).</summary>
    public string PositionReference { get; }

    // ── Optional point references ─────────────────────────────────────────

    /// <summary>CSWI.Loc.stVal — local/remote mode indicator. Null when not in model.</summary>
    public string? LocReference { get; }

    /// <summary>CSWI.OpCntRs.stVal — resettable operation counter. Null when not in model.</summary>
    public string? OpCntRsReference { get; }

    /// <summary>CSWI.OpOpn.stVal — transient open-operation indicator. Null when not in model.</summary>
    public string? OpOpnReference { get; }

    /// <summary>CSWI.OpCls.stVal — transient close-operation indicator. Null when not in model.</summary>
    public string? OpClsReference { get; }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reads current model values via <paramref name="readAttributeValue"/> and initializes
    /// optional DOs (Loc, OpCntRs) in the registry. Applies defaults for type-default values.
    /// </summary>
    public void Initialize(Func<DataAttribute, object?> readAttributeValue)
    {
        foreach (var pointRef in new[] { LocReference, OpCntRsReference }.Where(r => r != null))
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
    /// Sets Loc=<paramref name="value"/> on both CSWI.Loc and XCBR.Loc (when each ref exists).
    /// </summary>
    public void SyncLoc(bool value)
    {
        if (LocReference != null)
            _registry.SetValue(new PointValue<object>
            {
                Reference = LocReference,
                Value = value,
                Quality = 192,
                Timestamp = DateTimeOffset.UtcNow
            });

        if (SwitchDevice.LocReference != null)
            _registry.SetValue(new PointValue<object>
            {
                Reference = SwitchDevice.LocReference,
                Value = value,
                Quality = 192,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    public void BindSwitchDevice(ISwitchingDevice device) => SwitchDevice = device;

    public void BindCilo(CILO cilo) => Cilo = cilo;

    public void Operate(bool close)
    {
        if (close) SwitchDevice.Close();
        else SwitchDevice.Open();
    }

    public override void Step(double dt) { }

    private static bool IsTypeDefault(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrEmpty(s),
        int i => i == 0,
        bool b => !b,
        _ => true
    };
}