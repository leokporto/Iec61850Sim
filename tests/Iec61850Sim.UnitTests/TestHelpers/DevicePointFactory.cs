using System.Runtime.CompilerServices;
using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;

namespace Iec61850Sim.UnitTests.TestHelpers;

internal static class DevicePointFactory
{
    public static DevicePoint CreatePoint(
        string reference,
        string logicalNode,
        string dataObject,
        eLnType lnType,
        FunctionalConstraint fc,
        string equipmentName = "EQ",
        bool withValueAttribute = true)
    {
        return new DevicePoint
        {
            Reference = reference,
            EquipmentName = equipmentName,
            LogicalDevice = "LD0",
            LogicalNode = logicalNode,
            DataObject = dataObject,
            LnType = lnType,
            Fc = fc,
            ValueAttribute = withValueAttribute ? CreateDataAttribute() : null!
        };
    }

    public static DataAttribute CreateDataAttribute()
    {
        return (DataAttribute)RuntimeHelpers.GetUninitializedObject(typeof(DataAttribute));
    }
}
