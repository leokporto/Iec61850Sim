using IEC61850.Common;

namespace Iec61850Sim.Core.Model;

/// <summary>
/// Mutable value container for a single IEC 61850 data attribute.
/// The registry stores one instance per reference; devices mutate Value/Timestamp/Quality
/// in-place via PointRegistry.SetValue so all readers always see the current state.
/// Reference and FC are init-only structural fields that must not change after registration.
/// </summary>
public class PointValue<T>
{
    public string Reference { get; init; } = string.Empty;
    public FunctionalConstraint FC { get; init; }

    public T? Value { get; set; }
    public ushort Quality { get; set; } = 192;
    public DateTimeOffset Timestamp { get; set; }
}
