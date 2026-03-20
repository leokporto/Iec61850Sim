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

    public void Operate(CSWI controller, MmsValue ctlVal, bool test)
    {
        eDblPos pos;

        if (ctlVal.GetType() == MmsType.MMS_BOOLEAN)
            pos = ctlVal.GetBoolean() ? eDblPos.On : eDblPos.Off;
        else
            pos = (eDblPos)ctlVal.ToInt32();

        ApplyPosition(controller, pos);
    }

    public void Operate(CSWI controller, bool isSelected)
    {
        var pos = isSelected ? eDblPos.On : eDblPos.Off;
        ApplyPosition(controller, pos);
    }

    private void ApplyPosition(CSWI controller, eDblPos pos)
    {
        // Reject command if CSWI is in local mode
        if (controller.LocReference != null &&
            _registry.GetValue<object>(controller.LocReference).Value is true)
            return;

        var breaker = controller.Xcbr;

        switch (pos)
        {
            case eDblPos.Off:
                breaker.Open();
                UpdateOpTransients(controller, isClose: false);
                IncrementOpCntRs(controller);
                break;
            case eDblPos.On:
                breaker.Close();
                UpdateOpTransients(controller, isClose: true);
                IncrementOpCntRs(controller);
                break;
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

    private void UpdateOpTransients(CSWI controller, bool isClose)
    {
        if (controller.OpOpnReference != null)
            _registry.SetValue(new PointValue<object>
            {
                Reference = controller.OpOpnReference,
                Value = !isClose,
                Quality = 192,
                Timestamp = DateTimeOffset.UtcNow
            });

        if (controller.OpClsReference != null)
            _registry.SetValue(new PointValue<object>
            {
                Reference = controller.OpClsReference,
                Value = isClose,
                Quality = 192,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    private void IncrementOpCntRs(CSWI controller)
    {
        if (controller.OpCntRsReference == null)
            return;

        var current = _registry.GetValue<object>(controller.OpCntRsReference).Value;
        var count = current is int i ? i : 0;

        _registry.SetValue(new PointValue<object>
        {
            Reference = controller.OpCntRsReference,
            Value = count + 1,
            Quality = 192,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}