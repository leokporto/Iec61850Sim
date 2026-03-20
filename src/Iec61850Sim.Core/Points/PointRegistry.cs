using IEC61850.Common;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Points;

/// <summary>
/// Single source of truth for all device point values.
/// Stores DevicePoint objects (structural metadata + value state).
/// All value mutations must go through SetValue / SetValues — never through direct DevicePoint mutation.
/// </summary>
public class PointRegistry : IPointRegistry
{
    private readonly Dictionary<string, DevicePoint> _points = new();

    public IEnumerable<DevicePoint> All => _points.Values;

    public void Register(DevicePoint point)
    {
        _points[point.Reference] = point;
    }

    // ── Structural access ─────────────────────────────────────────────────

    public DevicePoint? GetPoint(string reference)
    {
        _points.TryGetValue(reference, out var p);
        return p;
    }

    /// <summary>Backward-compat alias for GetPoint.</summary>
    public DevicePoint? Get(string reference) => GetPoint(reference);

    public IEnumerable<DevicePoint> GetByFc(FunctionalConstraint fc)
        => _points.Values.Where(p => p.Fc == fc);

    // ── Single value access ───────────────────────────────────────────────

    public PointValue<T> GetValue<T>(string reference)
    {
        if (!_points.TryGetValue(reference, out var dp))
            throw new KeyNotFoundException($"No point registered for reference '{reference}'.");

        return new PointValue<T>
        {
            Reference = dp.Reference,
            FC = dp.Fc,
            Value = dp.Value is T typed ? typed : default,
            Quality = dp.Quality,
            Timestamp = dp.Timestamp
        };
    }

    public void SetValue<T>(PointValue<T> pointValue)
    {
        if (!_points.TryGetValue(pointValue.Reference, out var dp))
            throw new KeyNotFoundException($"No point registered for reference '{pointValue.Reference}'.");

        dp.Value = pointValue.Value;
        dp.Quality = pointValue.Quality;
        dp.Timestamp = pointValue.Timestamp;
    }

    // ── Batch value access ────────────────────────────────────────────────

    public IReadOnlyList<PointValue<object>> GetValues(IEnumerable<string> references)
        => references.Select(r => GetValue<object>(r)).ToList();

    public void SetValues(IEnumerable<PointValue<object>> values)
    {
        foreach (var v in values)
            SetValue(v);
    }
}