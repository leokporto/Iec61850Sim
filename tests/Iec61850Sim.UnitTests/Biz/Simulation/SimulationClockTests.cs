using Iec61850Sim.Core.Biz.Simulation;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Simulation;

public class SimulationClockTests
{
    [Fact]
    public void Tick_WithRegisteredTasks_ShouldInvokeAllWithSameDt()
    {
        // Arrange
        var clock = new SimulationClock();
        var firstCalls = 0;
        var secondCalls = 0;
        var firstDt = 0.0;
        var secondDt = 0.0;

        clock.Register(dt =>
        {
            firstCalls++;
            firstDt = dt;
        });
        clock.Register(dt =>
        {
            secondCalls++;
            secondDt = dt;
        });

        // Act
        clock.Tick(0.5);

        // Assert
        Assert.Equal(1, firstCalls);
        Assert.Equal(1, secondCalls);
        Assert.Equal(0.5, firstDt);
        Assert.Equal(0.5, secondDt);
    }

    [Fact]
    public void Tick_WithNoRegisteredTasks_ShouldNotThrow()
    {
        // Arrange
        var clock = new SimulationClock();

        // Act
        var exception = Record.Exception(() => clock.Tick(0.2));

        // Assert
        Assert.Null(exception);
    }
}
