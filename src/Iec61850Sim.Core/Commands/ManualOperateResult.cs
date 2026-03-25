namespace Iec61850Sim.Core.Commands;

/// <summary>
/// Result returned by <see cref="ManualOperationService.Execute"/>.
/// Carries a human-readable message for display in the UI.
/// </summary>
public record ManualOperateResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ManualOperateResult Ok(string message = "Operation successful.") =>
        new() { Success = true, Message = message };

    public static ManualOperateResult Fail(string message) =>
        new() { Success = false, Message = message };
}
