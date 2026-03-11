using IEC61850.Common;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Devices;

namespace Iec61850Sim.Core.Biz.Device;

public class DeviceBuilder
{
    public void Build(PointRegistry registry, DeviceManager manager)
    {
        BuildBreakers(registry, manager);
        BuildMeasurements(registry, manager);
        BuildControllers(registry, manager);
    }

    private void BuildBreakers(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.XCBR && p.DataObject == "Pos")
            .GroupBy(p => p.LogicalNode);

        foreach (var g in groups)
        {
            var point = g.First(x => x.Reference.Contains("stVal"));
            var breaker = new Breaker(g.Key, point);

            manager.Register(breaker);
        }
    }

    private void BuildMeasurements(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.MMXU)
            .GroupBy(p => p.LogicalNode);

        foreach (var group in groups)
        {
            var device = new MeasurementDevice(group.Key, group);

            manager.Register(device);
        }
    }

    private void BuildControllers(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.CSWI && p.Fc == FunctionalConstraint.CO )
            .GroupBy(p => p.LogicalNode);

        foreach (var g in groups)
        {
            var point = g.First(x => x.Reference.Contains("ctlVal"));
            
            var breaker = manager.Breakers.FirstOrDefault(x => x.Position.EquipmentName.Equals(point.EquipmentName));
            if(breaker == null)
            {
                continue;
            }

            var cswi = new Cswi(g.Key, breaker);

            manager.RegisterController(cswi);
        }
    }
}