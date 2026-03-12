using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Devices;

namespace Iec61850Sim.Core.Biz.Commands;

public class ControlCommandProcessor
{
    
    public ControlCommandProcessor()
    {

    }

    public void Operate(Cswi controller, MmsValue ctlVal, bool test)
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
        breaker.Position.Timestamp = DateTime.UtcNow;

        switch (pos)
        {
            case eDblPos.Off:
                breaker.Open();
                break;

            case eDblPos.On:
                breaker.Close();
                break;            
        }

        controller.CommandController.Value = breaker.Position.Value;
        controller.CommandController.Timestamp = breaker.Position.Timestamp;
        controller.Position.Value = breaker.Position.Value;
        controller.Position.Timestamp = breaker.Position.Timestamp;        
    }

    public void Operate(Cswi controller, bool isSelected)
    {
        eDblPos pos = isSelected ? eDblPos.On : eDblPos.Off;        

        var breaker = controller.Breaker;
        breaker.Position.Timestamp = DateTime.UtcNow;

        switch (pos)
        {
            case eDblPos.Off:
                breaker.Open();
                break;

            case eDblPos.On:
                breaker.Close();
                break;
        }

        controller.Position.Value = breaker.Position.Value;
        controller.Position.Timestamp = breaker.Position.Timestamp;
    }
}