using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Commands;

public class ControlCommandProcessor
{
    private readonly PointRegistry _registry;

    public ControlCommandProcessor(PointRegistry registry)
    {
        _registry = registry;
    }

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

    private void ApplyPosition(Cswi controller, eDblPos pos)
    {
        var breaker = controller.Breaker;

        switch (pos)
        {
            case eDblPos.Off:
                breaker.Open();
                break;
            case eDblPos.On:
                breaker.Close();
                break;
            // Intermediate / BadState: leave breaker state unchanged
        }

        // Sync CSWI.Pos.stVal with current breaker value (including its timestamp)
        var breakerValue = _registry.GetValue<object>(breaker.PositionReference);
        _registry.SetValue(new PointValue<object>
        {
            Reference = controller.PositionReference,
            Value = breakerValue.Value,
            Timestamp = breakerValue.Timestamp,
            Quality = breakerValue.Quality
        });
    }
}