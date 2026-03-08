using IEC61850.Common;
using IEC61850.Server;

namespace Iec61850Sim.Web.Model;

public class DevicePoint
{
    public string Reference { get; init; }

    public DataAttribute Attribute { get; init; }

    public string LogicalDevice { get; init; }
    public string LogicalNode { get; init; }
    public string DataObject { get; init; }
    public string LnType { get; init; }
    
    public FunctionalConstraint Fc { get; init; }

    public object? Value { get; set; }

}