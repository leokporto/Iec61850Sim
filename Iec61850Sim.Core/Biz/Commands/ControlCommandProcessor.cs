using IEC61850.Common;
using IEC61850.Server;
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

    public ControlHandlerResult Operate(DataObject obj, MmsValue ctlVal, bool test)
    {
        //int pos = ctlVal.GetInt32();

        //var breaker = ResolveBreaker(obj);

        //if (breaker == null)
        //    return ControlHandlerResult.FAILED;

        //switch (pos)
        //{
        //    case 1:
        //        breaker.Open();
        //        break;

        //    case 2:
        //        breaker.Close();
        //        break;

        //    default:
        //        return ControlHandlerResult.FAILED;
        //}

        return ControlHandlerResult.OK;
    }
}