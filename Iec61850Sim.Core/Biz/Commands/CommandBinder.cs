using IEC61850.Server;
using Iec61850Sim.Core.Model.Devices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iec61850Sim.Core.Biz.Commands;

public class CommandBinder
{
    private readonly Dictionary<DataObject, DeviceBase> bindings = new();

    public void Bind(DataObject controlObject, DeviceBase device)
    {
        bindings[controlObject] = device;
    }

    public bool TryResolve(DataObject controlObject, out DeviceBase device)
    {
        return bindings.TryGetValue(controlObject, out device);
    }
}