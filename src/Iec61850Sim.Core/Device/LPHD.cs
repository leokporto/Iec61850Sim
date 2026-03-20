using IEC61850.Server;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

/// <summary>
/// Represents a Physical Device logical node (LPHD). Static device — no simulation loop.
/// Initializes its registry values once from the model's current attribute values,
/// falling back to hardcoded defaults when the model value is a type default.
/// </summary>
public class LPHD : DeviceBase
{
    private static readonly Dictionary<string, object> Defaults = new()
    {
        ["PhyNam.vendor"] = "IEC61850Sim",
        ["PhyNam.swRev"] = "v1.0.0",
        ["PhyNam.hwRev"] = "v1.0.0",
        ["PhyNam.serNum"] = "SIM-001",
        ["PhyHealth.stVal"] = 1,
        ["Proxy.stVal"] = false
    };

    private readonly IReadOnlyList<string> _pointRefs;
    private readonly PointRegistry _registry;

    public LPHD(string logicalDevice, IReadOnlyList<string> pointRefs, PointRegistry registry)
        : base($"LPHD_{logicalDevice}")
    {
        _pointRefs = pointRefs;
        _registry = registry;
    }

    /// <summary>
    /// Reads current attribute values via <paramref name="readAttributeValue"/>.
    /// Uses hardcoded defaults only when the model value is a type default (null, empty, 0, false).
    /// </summary>
    public void Initialize(Func<DataAttribute, object?> readAttributeValue)
    {
        foreach (var pointRef in _pointRefs)
        {
            var point = _registry.GetPoint(pointRef);
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
                Reference = pointRef,
                Value = effectiveValue,
                Quality = 192,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    public override void Step(double dt) { } // Static device — no simulation

    private static bool IsTypeDefault(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrEmpty(s),
        int i => i == 0,
        bool b => !b,
        _ => true
    };
}