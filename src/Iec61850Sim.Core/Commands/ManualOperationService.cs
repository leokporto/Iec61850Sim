using IEC61850.Common;
using Iec61850Sim.Core.Device;

namespace Iec61850Sim.Core.Commands;

/// <summary>
/// Shared pipeline for manual switch operations triggered from the UI.
/// Applies the same rule set as SCADA commands (Loc, CILO, LLN0.Mod, BlkOpn/BlkCls,
/// PositionReached) by delegating to <see cref="ControlCommandProcessor"/>.
///
/// XSWI behaviour when simulation is stopped:
///   The operation is <b>blocked</b> and a descriptive message is returned.
///   Rationale: XSWI completes its maneuver via Step(dt) in the simulation loop.
///   Without simulation running the switch would be permanently stuck in Intermediate
///   state and the SwAbm alarm would fire, leaving the model in a corrupt state.
/// </summary>
public class ManualOperationService
{
    private readonly ControlCommandProcessor _processor;
    private readonly IOperationRuntime _runtime;

    public ManualOperationService(ControlCommandProcessor processor, IOperationRuntime runtime)
    {
        _processor = processor;
        _runtime = runtime;
    }

    /// <summary>
    /// Executes a manual open (<paramref name="isClose"/>=false) or close (<paramref name="isClose"/>=true)
    /// on the given <paramref name="controller"/>.
    /// </summary>
    public ManualOperateResult Execute(CSWI controller, bool isClose)
    {
        // XSWI guard: maneuver completes via simulation loop — block when simulation is stopped.
        if (controller.SwitchDevice is XSWI && !_runtime.SimulationRunning)
            return ManualOperateResult.Fail(
                "Cannot operate disconnector (XSWI): simulation is not running. " +
                "Start the simulation first so the maneuver can complete.");

        var result = _processor.Operate(controller, isClose);

        if (!result.Success)
            return ManualOperateResult.Fail(FormatCause(result.Cause));

        // Push updated values to the IEC 61850 server immediately.
        if (_runtime.ServerRunning)
            _runtime.Publish();

        return ManualOperateResult.Ok(isClose ? "Close command accepted." : "Open command accepted.");
    }

    private static string FormatCause(ControlAddCause cause) => cause switch
    {
        ControlAddCause.ADD_CAUSE_BLOCKED_BY_SWITCHING_HIERARCHY =>
            "Blocked: device is in Local mode.",
        ControlAddCause.ADD_CAUSE_BLOCKED_BY_INTERLOCKING =>
            "Blocked by interlocking (CILO or BlkOpn/BlkCls).",
        ControlAddCause.ADD_CAUSE_POSITION_REACHED =>
            "Position already reached — no operation needed.",
        ControlAddCause.ADD_CAUSE_BLOCKED_BY_MODE =>
            "Blocked: logical node mode is not On.",
        _ =>
            $"Operation failed ({cause})."
    };
}
