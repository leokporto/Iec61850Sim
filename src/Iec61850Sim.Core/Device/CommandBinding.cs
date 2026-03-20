using IEC61850.Server;

namespace Iec61850Sim.Core.Device;

public class CommandBinding
{
    public DataObject ControlObject { get; }
    public DeviceBase Device { get; }

    public CommandBinding(DataObject controlObject, DeviceBase device)
    {
        ControlObject = controlObject;
        Device = device;
    }
}