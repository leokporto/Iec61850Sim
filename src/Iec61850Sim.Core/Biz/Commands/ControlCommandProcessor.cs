using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Devices;

namespace Iec61850Sim.Core.Biz.Commands;


public class ControlCommandProcessor
{
    public void Operate(Cswi controller, MmsValue ctlVal, bool test)
    {
        eDblPos pos;

        if (ctlVal.GetType() == MmsType.MMS_BOOLEAN)
            pos = ctlVal.GetBoolean() ? eDblPos.On : eDblPos.Off;
        else
            pos = (eDblPos)ctlVal.ToInt32();

        ApplyPosition(controller, pos);
    }

    public void Operate(Cswi controller, bool isSelected)
    {
        var pos = isSelected ? eDblPos.On : eDblPos.Off;
        ApplyPosition(controller, pos);
    }

    private static void ApplyPosition(Cswi controller, eDblPos pos)
    {
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

        // Sincroniza o stVal do CSWI com o estado resultante do breaker
        controller.Position.Value = breaker.Position.Value;
        controller.Position.Timestamp = breaker.Position.Timestamp;
    }
}
