using Iec61850Sim.Core.Model;

namespace Iec61850Sim.Core.Biz.Points;

public interface IPointRegistry
{
    IEnumerable<DevicePoint> All { get; }
    DevicePoint? Get(string reference);
}
