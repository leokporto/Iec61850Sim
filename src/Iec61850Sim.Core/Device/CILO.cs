using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Device;

/// <summary>
/// Interlock (CILO on IEC 61850).
/// Guards a switching object by exposing EnaOpn/EnaCls enable conditions.
/// When a reference is absent the corresponding operation is permitted (permissive default).
/// </summary>
public class CILO : DeviceBase
{
    public string EquipmentName { get; }
    public string? EnaOpnReference { get; }
    public string? EnaClsReference { get; }

    public CILO(string name, string equipmentName, string? enaOpnRef = null, string? enaClsRef = null)
        : base(name)
    {
        EquipmentName = equipmentName;
        EnaOpnReference = enaOpnRef;
        EnaClsReference = enaClsRef;
    }

    /// <summary>Returns true when opening is permitted (EnaOpn absent or TRUE).</summary>
    public bool CanOpen(IPointRegistry registry)
    {
        if (EnaOpnReference == null) return true;
        return registry.GetValue<object>(EnaOpnReference).Value is not false;
    }

    /// <summary>Returns true when closing is permitted (EnaCls absent or TRUE).</summary>
    public bool CanClose(IPointRegistry registry)
    {
        if (EnaClsReference == null) return true;
        return registry.GetValue<object>(EnaClsReference).Value is not false;
    }

    public override void Step(double dt) { }
}
