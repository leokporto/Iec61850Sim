using Iec61850Sim.Core.Simulation;
using Xunit;

namespace Iec61850Sim.UnitTests.Simulation;

public class ScenarioEngineTests
{
    [Fact]
    public void Register_AndRun_WithExistingScenario_ShouldExecuteAction()
    {
        // Arrange
        var engine = new ScenarioEngine();
        var executed = false;
        engine.Register("s1", () => executed = true);

        // Act
        engine.Run("s1");

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void Register_WithSameName_ShouldOverridePreviousAction()
    {
        // Arrange
        var engine = new ScenarioEngine();
        var firstCalls = 0;
        var secondCalls = 0;
        engine.Register("s1", () => firstCalls++);
        engine.Register("s1", () => secondCalls++);

        // Act
        engine.Run("s1");

        // Assert
        Assert.Equal(0, firstCalls);
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public void Run_WithUnknownScenario_ShouldDoNothing()
    {
        // Arrange
        var engine = new ScenarioEngine();

        // Act
        var exception = Record.Exception(() => engine.Run("unknown"));

        // Assert
        Assert.Null(exception);
    }
}