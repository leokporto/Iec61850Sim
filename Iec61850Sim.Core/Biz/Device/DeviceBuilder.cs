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
            .Where(p => p.LnType == eLnType.XCBR && p.DataObject == "Pos"
            && p.Fc == FunctionalConstraint.ST && p.ValueAttribute != null)
            .GroupBy(p => p.LogicalNode);
        
        foreach (var g in groups)
        {
            //var point = g.First(x => x.Fc == FunctionalConstraint.ST);
            var point = g.Single();
            var breaker = new Breaker(g.Key, point);

            manager.Register(breaker);
        }       
    }

    private void BuildMeasurements(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.MMXU
                    && p.Fc == FunctionalConstraint.MX 
                    && p.ValueAttribute != null)
            .GroupBy(p => p.LogicalNode);

        foreach (var group in groups)
        {
            var device = new MeasurementDevice(group.Key, group);

            manager.Register(device);
        }
        
    }

    //private void BuildControllers(PointRegistry registry, DeviceManager manager)
    //{
    //    var groups = registry.All
    //        .Where(p => p.LnType == eLnType.CSWI && p.Fc == FunctionalConstraint.CO )
    //        .GroupBy(p => p.LogicalNode);

    //    foreach (var g in groups)
    //    {
    //        var bindPoint = g.First(); //XCBR STVAL

    //        var breaker = manager.Breakers.FirstOrDefault(x => x.Position.EquipmentName.Equals(bindPoint.EquipmentName));
    //        if (breaker == null || string.IsNullOrWhiteSpace(breaker.Name))
    //        {                    
    //            continue;
    //        }

    //        var cswi = new Cswi(g.Key, bindPoint, breaker);

    //        manager.RegisterController(cswi);
    //    }
    //}

    private void BuildControllers(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.CSWI)
            .GroupBy(p => p.LogicalNode);

        foreach (var g in groups)
        {
            var cmdPoint = g.FirstOrDefault(p => p.Fc == FunctionalConstraint.CO);
            var posPoint = g.FirstOrDefault(p => p.Fc == FunctionalConstraint.ST &&
                                                 p.DataObject == "Pos" &&
                                                 p.ValueAttribute != null);

            if (cmdPoint == null || posPoint == null)
                continue;

            var breaker = manager.Breakers
                .FirstOrDefault(x => x.Position.EquipmentName.Equals(cmdPoint.EquipmentName));

            if (breaker == null)
                continue;

            var cswi = new Cswi(g.Key, cmdPoint, posPoint, breaker);

            manager.RegisterController(cswi);
        }
    }
}