using IEC61850.Common;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Points;

/// <summary>
/// Registers and maintains device Points
/// </summary>
public class PointRegistry : IPointRegistry
{
    private readonly Dictionary<string, DevicePoint> points = new();

    public IEnumerable<DevicePoint> All => points.Values;
    
    public void Register(DevicePoint point)
    {
        points[point.Reference] = point;
    }

    
    public DevicePoint? Get(string reference)
    {
        points.TryGetValue(reference, out var p);
        return p;

    }
    
    public IEnumerable<DevicePoint> GetByFc(FunctionalConstraint fc)
    {
        return points.Values.Where(p => p.Fc == fc);
    }

}