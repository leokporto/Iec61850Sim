using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Web.Iec61850;
using Iec61850Sim.Web.Model;

namespace Iec61850Sim.Web.Model;

public class ModelScanner
{
    public void Scan(IedModel model, PointRegistry registry, IEnumerable<string> logicalDevices)
    {
        var points = new Dictionary<string, DevicePoint>();

        foreach (var ldName in logicalDevices)
        {
            var ld = model.GetDeviceByInst(ldName);

            if (ld == null)
                continue;

            ScanNode(ld, points);
        }

        foreach (var p in points.Values)
            registry.Register(p);
    }

    private void ScanNode(ModelNode node, Dictionary<string, DevicePoint> points)
    {
        var children = node.GetChildren();

        if (children == null)
            return;

        foreach (ModelNode child in children)
        {
            if (child is DataAttribute da)
            {
                if (da.GetChildren()?.Any() != true)
                    ProcessLeafAttribute(da, points);
            }

            ScanNode(child, points);
        }
    }

    private void ProcessLeafAttribute(DataAttribute da, Dictionary<string, DevicePoint> points)
    {
        ExtractNodeInfo(da,
            out var ld,
            out var ln,
            out var lnType,
            out var dataObject);

        var reference = da.GetObjectReference();
        var baseReference = $"{ld}/{ln}.{dataObject}";

        if (!points.TryGetValue(baseReference, out var point))
        {
            point = new DevicePoint
            {
                Reference = reference,
                BaseReference = baseReference,
                LogicalDevice = ld,
                LogicalNode = ln,
                LnType = lnType,
                DataObject = dataObject
            };

            points[baseReference] = point;
        }

        ClassifyAttribute(point, da);
    }
    
    private static void ExtractNodeInfo(
        DataAttribute da,
        out string logicalDevice,
        out string logicalNode,
        out eLnType lnType,
        out string dataObject)
    {
        logicalDevice = "";
        logicalNode = "";
        lnType = eLnType.Unknown;
        dataObject = "";

        ModelNode current = da;

        while (current != null)
        {
            switch (current)
            {
                case DataObject dob:
                    // Captura apenas o DO mais próximo do DataAttribute
                    if (string.IsNullOrEmpty(dataObject))
                        dataObject = dob.GetName();
                    break;

                case LogicalNode ln:
                    logicalNode = ln.GetName();
                    lnType = ExtractLnType(logicalNode);

                    if (ln.GetParent() is LogicalDevice ld)
                        logicalDevice = ld.GetName();

                    return;
            }

            current = current.GetParent();
        }
    }
    static readonly HashSet<string> ValueNames =
    [
        "stVal",
        "ctlVal",
        "setVal",
        "mag",
        "instMag",
        "f"
    ];

    private static void ClassifyAttribute(DevicePoint point, DataAttribute da)
    {
        var type = da.Type;
        var name = da.GetName();

        if (type == DataAttributeType.QUALITY)
        {
            point.QualityAttribute = da;
            return;
        }

        if (type == DataAttributeType.TIMESTAMP ||
            type == DataAttributeType.ENTRY_TIME)
        {
            point.TimestampAttribute = da;
            return;
        }

        if (ValueNames.Contains(name))
        {
            point.ValueAttribute ??= da;
            return;
        }

        point.OtherAttributes.Add(da);
    }
    private static eLnType ExtractLnType(string logicalNode)
    {
        foreach (var kv in KnownLnTypes)
        {
            if (logicalNode.Contains(kv.Key))
                return kv.Value;
        }

        return eLnType.Unknown;
    }
    private static readonly Dictionary<string, eLnType> KnownLnTypes =
        new()
        {
            ["MMXU"] = eLnType.MMXU,
            ["XCBR"] = eLnType.XCBR,
            ["XSWI"] = eLnType.XSWI,
            ["CSWI"] = eLnType.CSWI,
            ["GGIO"] = eLnType.GGIO,
            ["PTOC"] = eLnType.PTOC,
            ["PTRC"] = eLnType.PTRC,
            ["PDIS"] = eLnType.PDIS,
            ["CILO"] = eLnType.CILO,
            ["LLN0"] = eLnType.LLN0,
            ["LPHD"] = eLnType.LPHD
        };

}

/*
 
public class ModelScanner
{
    public void Scan(IedModel model, PointRegistry registry, IEnumerable<string> logicalDevices)
    {
        var points = new Dictionary<string, DevicePoint>();

        foreach (var ldName in logicalDevices)
        {
            var ld = model.GetDeviceByInst(ldName);

            if (ld == null)
                continue;

            ScanNode(ld, points);
        }

        foreach (var p in points.Values)
            registry.Register(p);
    }

    private void ScanNode(ModelNode node, Dictionary<string, DevicePoint> points)
    {
        var children = node.GetChildren();

        if (children == null)
            return;

        foreach (ModelNode child in children)
        {
            if (child is DataAttribute da)
            {
                var childNodes = da.GetChildren();

                // Apenas folhas
                if (childNodes == null || !childNodes.Any())
                    ProcessLeafAttribute(da, points);
            }

            ScanNode(child, points);
        }
    }

    private void ProcessLeafAttribute(DataAttribute da, Dictionary<string, DevicePoint> points)
    {
        var reference = da.GetObjectReference();

        ParseReference(reference,
            out var ld,
            out var ln,
            out var lnType,
            out var dataObject);

        var baseReference = $"{ld}/{ln}.{dataObject}";

        if (!points.TryGetValue(baseReference, out var point))
        {
            
            point = new DevicePoint
            {
                Reference = reference,
                BaseReference = baseReference,
                LogicalDevice = ld,
                LogicalNode = ln,
                LnType = lnType,
                DataObject = dataObject
            };

            points[baseReference] = point;
        }

        var type = da.Type;

        if (type == DataAttributeType.QUALITY)
        {
            point.QualityAttribute = da;
            return;
        }

        if (type == DataAttributeType.TIMESTAMP)
        {
            point.TimestampAttribute = da;
            return;
        }

        if (IsNumeric(type))
        {
            point.ValueAttribute ??= da;
            return;
        }

        point.OtherAttributes.Add(da);
    }

    private static bool IsNumeric(DataAttributeType type)
    {
        return type == DataAttributeType.FLOAT32 ||
               type == DataAttributeType.FLOAT64 ||
               type == DataAttributeType.INT8 ||
               type == DataAttributeType.INT16 ||
               type == DataAttributeType.INT32 ||
               type == DataAttributeType.INT64 ||
               type == DataAttributeType.INT8U ||
               type == DataAttributeType.INT16U ||
               type == DataAttributeType.INT32U;
    }

    private static void ParseReference(
        string reference,
        out string logicalDevice,
        out string logicalNode,
        out eLnType lnType,
        out string dataObject)
    {
        logicalDevice = "";
        logicalNode = "";
        lnType = eLnType.Unknown;
        dataObject = "";

        var parts = reference.Split('/');

        if (parts.Length < 2)
            return;

        logicalDevice = parts[0];

        var nodeParts = parts[1].Split('.');

        if (nodeParts.Length == 0)
            return;

        logicalNode = nodeParts[0];

        lnType = ExtractLnType(logicalNode);

        if (nodeParts.Length > 1)
            dataObject = nodeParts[1];
    }

    private static readonly Dictionary<string, eLnType> KnownLnTypes =
        new()
        {
            ["MMXU"] = eLnType.MMXU,
            ["XCBR"] = eLnType.XCBR,
            ["XSWI"] = eLnType.XSWI,
            ["CSWI"] = eLnType.CSWI,
            ["GGIO"] = eLnType.GGIO,
            ["PTOC"] = eLnType.PTOC,
            ["PTRC"] = eLnType.PTRC,
            ["PDIS"] = eLnType.PDIS,
            ["CILO"] = eLnType.CILO,
            ["LLN0"] = eLnType.LLN0,
            ["LPHD"] = eLnType.LPHD
        };

    private static eLnType ExtractLnType(string logicalNode)
    {
        foreach (var kv in KnownLnTypes)
        {
            if (logicalNode.Contains(kv.Key))
                return kv.Value;
        }

        return eLnType.Unknown;
    }
}
*/