using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Devices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iec61850Sim.Core.Biz.Commands;

public class ControlCommandProcessor
{
    private readonly CommandBinder binder;

    public ControlCommandProcessor(CommandBinder binder)
    {
        this.binder = binder;
    }

    public ControlHandlerResult Operate(Cswi controller, MmsValue ctlVal, bool test)
    {
        eDblPos pos;

        if (ctlVal.GetType() == MmsType.MMS_BOOLEAN)
        {
            pos = ctlVal.GetBoolean() ? eDblPos.On : eDblPos.Off;
        }
        else
        {
            pos = (eDblPos)ctlVal.ToInt32();
        }

        var breaker = controller.Breaker;

        switch (pos)
        {
            case eDblPos.Off:
                breaker.Open();
                break;

            case eDblPos.On:
                breaker.Close();
                break;

            default:
                return ControlHandlerResult.FAILED;
        }

        return ControlHandlerResult.OK;
    }
}