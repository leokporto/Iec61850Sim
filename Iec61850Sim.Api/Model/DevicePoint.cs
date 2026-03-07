using IEC61850.Common;
using IEC61850.Server;

namespace Iec61850Sim.Api.Model;

public class DevicePoint
{
    public string Reference { get; init; }

    public DataAttribute Attribute { get; init; }

    public FunctionalConstraint Fc { get; init; }

    public object? Value { get; set; }

}