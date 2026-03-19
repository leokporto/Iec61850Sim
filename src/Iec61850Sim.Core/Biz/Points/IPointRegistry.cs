using IEC61850.Common;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Points;

public interface IPointRegistry
{
    IEnumerable<DevicePoint> All { get; }

    // Structural
    DevicePoint? GetPoint(string reference);
    IEnumerable<DevicePoint> GetByFc(FunctionalConstraint fc);

    // Typed value access (single)
    PointValue<T> GetValue<T>(string reference);
    void SetValue<T>(PointValue<T> pointValue);

    // Batch value access
    IReadOnlyList<PointValue<object>> GetValues(IEnumerable<string> references);
    void SetValues(IEnumerable<PointValue<object>> values);
}
