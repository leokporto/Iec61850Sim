namespace Iec61850Sim.Core.Commands;

/// <summary>
/// Minimal runtime state needed by <see cref="ManualOperationService"/>.
/// Extracted so the service can be tested without the full <c>RuntimeEngine</c> dependency tree.
/// </summary>
public interface IOperationRuntime
{
    bool SimulationRunning { get; }
    bool ServerRunning { get; }
    void Publish();
}
