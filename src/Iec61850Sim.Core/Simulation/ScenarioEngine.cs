namespace Iec61850Sim.Core.Simulation;

public class ScenarioEngine
{
    private readonly Dictionary<string, Action> scenarios = new();

    public void Register(string name, Action action)
        => scenarios[name] = action;

    public void Run(string name)
    {
        if (scenarios.TryGetValue(name, out var s))
            s();
    }
}