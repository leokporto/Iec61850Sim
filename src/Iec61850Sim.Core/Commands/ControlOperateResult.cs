using IEC61850.Common;

namespace Iec61850Sim.Core.Commands;

/// <summary>
/// Result returned by <see cref="ControlCommandProcessor.Operate"/> methods.
/// Signals success or the specific IEC 61850 add-cause for the failure so that
/// <c>IecServerHost.ControlHandler</c> can propagate the correct MMS response.
/// </summary>
public record ControlOperateResult
{
    public bool Success { get; init; }
    public ControlAddCause Cause { get; init; }

    public static ControlOperateResult Ok() =>
        new() { Success = true, Cause = ControlAddCause.ADD_CAUSE_NONE };

    public static ControlOperateResult Fail(ControlAddCause cause) =>
        new() { Success = false, Cause = cause };
}
