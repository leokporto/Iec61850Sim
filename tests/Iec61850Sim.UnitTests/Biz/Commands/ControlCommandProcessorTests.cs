using IEC61850.Common;
using Iec61850Sim.Core.Biz.Commands;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Devices;
using Iec61850Sim.UnitTests.TestHelpers;
using Xunit;

namespace Iec61850Sim.UnitTests.Biz.Commands;

public class ControlCommandProcessorTests
{
    [Fact]
    public void Operate_WithBooleanTrue_ShouldCloseBreakerAndSyncControllerPosition()
    {
        // Arrange
        var sut = new ControlCommandProcessor();
        var controller = CreateController();
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        sut.Operate(controller, new MmsValue(true), test: false);

        // Assert
        Assert.Equal(2, controller.Breaker.Position.Value);
        Assert.Equal(controller.Breaker.Position.Value, controller.Position.Value);
        Assert.True(controller.Breaker.Position.Timestamp >= before);
        Assert.Equal(controller.Breaker.Position.Timestamp, controller.Position.Timestamp);
    }

    [Fact]
    public void Operate_WithBooleanFalse_ShouldOpenBreakerAndSyncControllerPosition()
    {
        // Arrange
        var sut = new ControlCommandProcessor();
        var controller = CreateController();

        // Act
        sut.Operate(controller, new MmsValue(false), test: false);

        // Assert
        Assert.Equal(1, controller.Breaker.Position.Value);
        Assert.Equal(controller.Breaker.Position.Value, controller.Position.Value);
    }

    [Fact]
    public void Operate_WithIntegerOn_ShouldCloseBreaker()
    {
        // Arrange
        var sut = new ControlCommandProcessor();
        var controller = CreateController();

        // Act
        sut.Operate(controller, new MmsValue((int)eDblPos.On), test: false);

        // Assert
        Assert.Equal(2, controller.Breaker.Position.Value);
    }

    [Fact]
    public void Operate_WithIsSelectedFalse_ShouldOpenBreaker()
    {
        // Arrange
        var sut = new ControlCommandProcessor();
        var controller = CreateController();

        // Act
        sut.Operate(controller, isSelected: false);

        // Assert
        Assert.Equal(1, controller.Breaker.Position.Value);
        Assert.Equal(controller.Breaker.Position.Value, controller.Position.Value);
    }

    [Fact]
    public void Operate_WithIntermediatePosition_ShouldKeepCurrentBreakerValue()
    {
        // Arrange
        var sut = new ControlCommandProcessor();
        var controller = CreateController();
        controller.Breaker.Position.Value = 2;

        // Act
        sut.Operate(controller, new MmsValue((int)eDblPos.Intermediate), test: false);

        // Assert
        Assert.Equal(2, controller.Breaker.Position.Value);
        Assert.Equal(2, controller.Position.Value);
    }

    private static Cswi CreateController()
    {
        var breakerPos = DevicePointFactory.CreatePoint(
            "LD0/XCBR1.Pos.stVal",
            "XCBR1",
            "Pos",
            eLnType.XCBR,
            FunctionalConstraint.ST);
        breakerPos.Value = 0;
        var breaker = new Breaker("XCBR1", breakerPos);

        var cmdPoint = DevicePointFactory.CreatePoint(
            "LD0/CSWI1.Pos.Oper.ctlVal",
            "CSWI1",
            "Pos",
            eLnType.CSWI,
            FunctionalConstraint.CO,
            withValueAttribute: false);
        var posPoint = DevicePointFactory.CreatePoint(
            "LD0/CSWI1.Pos.stVal",
            "CSWI1",
            "Pos",
            eLnType.CSWI,
            FunctionalConstraint.ST);

        return new Cswi("CSWI1", cmdPoint, posPoint, breaker);
    }
}
