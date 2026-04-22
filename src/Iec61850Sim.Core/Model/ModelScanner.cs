using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Model;



public class ModelScanner
{
    static readonly HashSet<string> ValueNames =
    [
        "stVal",
        "ctlVal",
        "setVal",
        "mag",
        "instMag",
        "f"
    ];

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
            ["LPHD"] = eLnType.LPHD,
            // Gravadores de perturbação: reconhecidos para permitir exclusão explícita do scan.
            ["RDRE"] = eLnType.RDRE,
            ["RADR"] = eLnType.RADR,
            ["RBDR"] = eLnType.RBDR,
            ["RDRS"] = eLnType.RDRS
        };

    // LNs de gravador de perturbação não possuem valores simuláveis (sem MX/ST de processo).
    // São excluídos do PointRegistry para não gerar DevicePoints sem sentido de simulação.
    private static readonly HashSet<eLnType> LnTypesExcludedFromScan =
    [
        eLnType.RDRE, eLnType.RADR, eLnType.RBDR, eLnType.RDRS
    ];

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

    private static bool IsStringType(DataAttributeType type) =>
        type is DataAttributeType.VISIBLE_STRING_32
            or DataAttributeType.VISIBLE_STRING_64
            or DataAttributeType.VISIBLE_STRING_65
            or DataAttributeType.VISIBLE_STRING_129
            or DataAttributeType.VISIBLE_STRING_255;

    private void ProcessLeafAttribute(DataAttribute da, Dictionary<string, DevicePoint> points)
    {
        ExtractNodeInfo(da,
            out var ld,
            out var ln,
            out var lnType,
            out var dataObject,
            out var equipmentName);

        // LNs de gravador de perturbação são excluídos do scan (sem valores simuláveis).
        if (LnTypesExcludedFromScan.Contains(lnType))
            return;

        var reference = da.GetObjectReference();

        // String DAs each get their own key to avoid all NamPlt/PhyNam attributes collapsing
        // into one DevicePoint. Non-string DAs still group by DO+FC as before.
        var groupKey = IsStringType(da.Type)
            ? $"{ld}/{ln}.{dataObject}.{da.GetName()}[{da.FC}]"
            : $"{ld}/{ln}.{dataObject}[{da.FC}]";

        if (!points.TryGetValue(groupKey, out var point))
        {
            point = new DevicePoint
            {
                Reference = reference,
                LogicalDevice = ld,
                LogicalNode = ln,
                LnType = lnType,
                DataObject = dataObject,
                EquipmentName = equipmentName,
                Fc = da.FC
            };

            points[groupKey] = point;
        }

        ClassifyAttribute(point, da);
    }

    private static void ExtractNodeInfo(
        DataAttribute da,
        out string logicalDevice,
        out string logicalNode,
        out eLnType lnType,
        out string dataObject,
        out string equipmentName)
    {
        logicalDevice = "";
        logicalNode = "";
        lnType = eLnType.Unknown;
        dataObject = "";
        equipmentName = "";

        ModelNode current = da;

        while (current != null)
        {
            switch (current)
            {
                case DataObject dob:
                    if (string.IsNullOrEmpty(dataObject))
                        dataObject = dob.GetName();
                    break;

                case LogicalNode ln:
                    logicalNode = ln.GetName();
                    lnType = ExtractLnType(logicalNode);
                    equipmentName = ExtractEquipment(logicalNode);

                    if (ln.GetParent() is LogicalDevice ld)
                        logicalDevice = ld.GetName();

                    return;
            }

            current = current.GetParent();
        }
    }

    private static void ClassifyAttribute(DevicePoint point, DataAttribute da)
    {
        var type = da.Type;
        var name = da.GetName();

        if (type == DataAttributeType.QUALITY)
        {
            point.QualityAttribute = da;
            return;
        }

        if (type == DataAttributeType.TIMESTAMP || type == DataAttributeType.ENTRY_TIME)
        {
            point.TimestampAttribute = da;
            return;
        }

        if (ValueNames.Contains(name) || IsStringType(type))
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

    private static string ExtractEquipment(string logicalNode)
    {
        var lnTypeIndex = logicalNode.Length;
        foreach (var kv in KnownLnTypes)
        {
            var index = logicalNode.IndexOf(kv.Key, StringComparison.Ordinal);
            if (index >= 0 && index < lnTypeIndex)
            {
                lnTypeIndex = index;
                break;
            }
        }

        return logicalNode.Substring(0, lnTypeIndex);
    }
}
