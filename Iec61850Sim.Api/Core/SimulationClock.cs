namespace Iec61850Sim.Api.Core;

public class SimulationClock
{
    private readonly List<Action<double>> tasks = new();

    public void Register(Action<double> task)
        => tasks.Add(task);

    public void Tick(double dt)
    {
        foreach (var t in tasks)
            t(dt);
    }
}