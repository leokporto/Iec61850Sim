using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;

namespace Iec61850Sim.Core.Model;

public class DevicePoint
{
    
    public string Reference { get; init; }
    public string EquipmentName { get; init; }

    public DataAttribute ValueAttribute { get; set; }
    
    public DataAttribute? QualityAttribute { get; set; }

    public DataAttribute? TimestampAttribute { get; set; }

    public List<DataAttribute> OtherAttributes { get; } = [];


    public string LogicalDevice { get; init; }
    public string LogicalNode { get; init; }
    public string DataObject { get; init; }
    public eLnType LnType { get; init; }
    
    public FunctionalConstraint Fc { get; init; }

    public object? Value { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public ushort Quality { get; set; } = 192;
    public Dictionary<string,object> OtherValues { get; } = new();

}