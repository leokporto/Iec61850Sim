namespace Iec61850Sim.Core.Simulation;

public class SimulationClock
{
    private readonly List<Action<double>> tasks = new();

    public void Register(Action<double> task)
        => tasks.Add(task);

    public void Clear() => tasks.Clear();

    public void Tick(double dt)
    {
        foreach (var t in tasks)
            t(dt);
    }
}