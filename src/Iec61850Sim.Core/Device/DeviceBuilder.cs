using IEC61850.Common;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

public class DeviceBuilder
{
    public void Build(PointRegistry registry, DeviceManager manager)
    {
        BuildBreakers(registry, manager);
        BuildMeasurements(registry, manager);
        BuildCilos(registry, manager);
        BuildControllers(registry, manager);
        BuildLLN0Devices(registry, manager);
        BuildLPHDDevices(registry, manager);
    }

    private void BuildBreakers(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.XCBR && p.Fc == FunctionalConstraint.ST && p.ValueAttribute != null)
            .GroupBy(p => p.LogicalNode);

        foreach (var g in groups)
        {
            var posPoint = g.Where(p => p.DataObject == "Pos").Single(); // throws on duplicates

            var breaker = new XCBR(g.Key, posPoint.Reference, posPoint.EquipmentName, registry,
                locRef: g.FirstOrDefault(p => p.DataObject == "Loc")?.Reference,
                blkOpnRef: g.FirstOrDefault(p => p.DataObject == "BlkOpn")?.Reference,
                blkClsRef: g.FirstOrDefault(p => p.DataObject == "BlkCls")?.Reference,
                opCntRef: g.FirstOrDefault(p => p.DataObject == "OpCnt")?.Reference,
                cbOpCapRef: g.FirstOrDefault(p => p.DataObject == "CBOpCap")?.Reference);

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
            var refs = group.Select(p => p.Reference);
            var device = new MeasurementDevice(group.Key, refs, registry);
            manager.Register(device);
        }
    }

    private void BuildLLN0Devices(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.LLN0 && p.ValueAttribute != null)
            .GroupBy(p => p.LogicalDevice);

        foreach (var g in groups)
        {
            var refs = g.Select(p => p.Reference).ToList();
            manager.Register(new LLN0(g.Key, refs, registry));
        }
    }

    private void BuildLPHDDevices(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.LPHD && p.ValueAttribute != null)
            .GroupBy(p => p.LogicalDevice);

        foreach (var g in groups)
        {
            var refs = g.Select(p => p.Reference).ToList();
            manager.Register(new LPHD(g.Key, refs, registry));
        }
    }

    private void BuildCilos(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p => p.LnType == eLnType.CILO && p.ValueAttribute != null)
            .GroupBy(p => p.LogicalNode);

        foreach (var g in groups)
        {
            var stPoints = g.Where(p => p.Fc == FunctionalConstraint.ST).ToList();
            var firstPoint = g.First();

            var cilo = new CILO(
                g.Key,
                firstPoint.EquipmentName,
                enaOpnRef: stPoints.FirstOrDefault(p => p.DataObject == "EnaOpn")?.Reference,
                enaClsRef: stPoints.FirstOrDefault(p => p.DataObject == "EnaCls")?.Reference);

            manager.RegisterCilo(cilo);
        }
    }

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
            {
                Console.WriteLine($"[DeviceBuilder] CSWI '{g.Key}' ignorado: cmdPoint={cmdPoint?.Reference ?? "null"}, posPoint={posPoint?.Reference ?? "null"}");
                continue;
            }

            var breaker = manager.Breakers
                .FirstOrDefault(x => x.EquipmentName.Equals(cmdPoint.EquipmentName));

            if (breaker == null)
                continue;

            var stPoints = g.Where(p => p.Fc == FunctionalConstraint.ST && p.ValueAttribute != null).ToList();

            var cswi = new CSWI(g.Key, cmdPoint.Reference, posPoint.Reference, breaker, registry,
                locRef: stPoints.FirstOrDefault(p => p.DataObject == "Loc")?.Reference,
                opCntRsRef: stPoints.FirstOrDefault(p => p.DataObject == "OpCntRs")?.Reference,
                opOpnRef: stPoints.FirstOrDefault(p => p.DataObject == "OpOpn")?.Reference,
                opClsRef: stPoints.FirstOrDefault(p => p.DataObject == "OpCls")?.Reference);

            // Link CILO by equipment name
            var cilo = manager.CILOs.FirstOrDefault(c => c.EquipmentName.Equals(cmdPoint.EquipmentName));
            if (cilo != null)
                cswi.BindCilo(cilo);

            manager.RegisterController(cswi);
        }
    }
}