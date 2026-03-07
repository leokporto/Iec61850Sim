using IEC61850.Common;
using Iec61850Sim.Api.Core.Model;
using Iec61850Sim.Api.Model;
using Iec61850Sim.Api.Model.Devices;

public class DeviceBuilder
{
    public void Build(PointRegistry registry, DeviceManager manager)
    {
        BuildBreakers(registry, manager);
        BuildMeasurements(registry, manager);
    }

    void BuildBreakers(PointRegistry registry, DeviceManager manager)
    {
        var breakerPoints = registry.All
            .Where(p =>
                p.Reference.Contains("Pos.stVal") &&
                ExtractLogicalNode(p.Reference).Contains("XCBR"));

        foreach (var p in breakerPoints)
        {
            var ln = ExtractLogicalNode(p.Reference);

            var breaker = new Breaker(ln, p);

            manager.Register(breaker);
        }
    }

    void BuildMeasurements(PointRegistry registry, DeviceManager manager)
    {
        var groups = registry.All
            .Where(p =>
                ExtractLogicalNode(p.Reference).Contains("MMXU"))
            .GroupBy(p => ExtractLogicalNode(p.Reference));

        foreach (var g in groups)
        {
            var device = new MeasurementDevice(g.Key, g);

            manager.Register(device);
        }
    }


    string ExtractLogicalNode(string reference)
    {
        var parts = reference.Split('/');

        if (parts.Length < 2)
            return "";

        var lnPart = parts[1];

        return lnPart.Split('.')[0];
    }
}