using IEC61850.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iec61850Sim.Core.Model.Devices;

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

