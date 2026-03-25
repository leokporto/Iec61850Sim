using IEC61850.Common;
using Iec61850Sim.Core.Commands;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Points;
using Iec61850Sim.UnitTests.TestHelpers;
using NSubstitute;
using Xunit;

namespace Iec61850Sim.UnitTests.Commands;

public class ManualOperationServiceTests
{
    // ── XSWI simulation-stopped guard ────────────────────────────────────

    [Fact]
    public void Execute_WhenXswiAndSimulationStopped_ShouldReturnFailure()
    {
        var (sut, _, controller, _) = CreateWithXswi(simulationRunning: false);

        var result = sut.Execute(controller, isClose: true);

        Assert.False(result.Success);
        Assert.Contains("simulation is not running", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_WhenXswiAndSimulationStopped_ShouldNotChangePosition()
    {
        var (sut, registry, controller, posRef) = CreateWithXswi(simulationRunning: false);
        SetValue(registry, posRef, 1);

        sut.Execute(controller, isClose: true);

        Assert.Equal(1, registry.GetValue<object>(posRef).Value);
    }

    [Fact]
    public void Execute_WhenXswiAndSimulationRunning_ShouldSetIntermediate()
    {
        // XSWI.Open/Close sets Intermediate immediately; completion comes via Step.
        var (sut, registry, controller, posRef) = CreateWithXswi(simulationRunning: true);
        SetValue(registry, posRef, 2); // currently closed

        sut.Execute(controller, isClose: false); // Open command

        Assert.Equal((int)eDblPos.Intermediate, registry.GetValue<object>(posRef).Value);
    }

    // ── XCBR — no simulation check ────────────────────────────────────────

    [Fact]
    public void Execute_WhenXcbrAndSimulationStopped_ShouldSucceed()
    {
        var (sut, registry, controller, posRef) = CreateWithXcbr(simulationRunning: false);
        SetValue(registry, posRef, 2); // currently closed

        var result = sut.Execute(controller, isClose: false); // Open

        Assert.True(result.Success);
        Assert.Equal(1, registry.GetValue<object>(posRef).Value);
    }

    // ── Success path ──────────────────────────────────────────────────────

    [Fact]
    public void Execute_WhenSuccessAndServerRunning_ShouldCallPublish()
    {
        var (sut, _, controller, _) = CreateWithXcbr(simulationRunning: false, serverRunning: true);
        var runtime = GetRuntime(sut);

        sut.Execute(controller, isClose: true);

        runtime.Received(1).Publish();
    }

    [Fact]
    public void Execute_WhenSuccessAndServerStopped_ShouldNotCallPublish()
    {
        var (sut, _, controller, _) = CreateWithXcbr(simulationRunning: false, serverRunning: false);
        var runtime = GetRuntime(sut);

        sut.Execute(controller, isClose: true);

        runtime.DidNotReceive().Publish();
    }

    [Fact]
    public void Execute_Close_ShouldReturnSuccessMessage()
    {
        var (sut, _, controller, _) = CreateWithXcbr(simulationRunning: false);

        var result = sut.Execute(controller, isClose: true);

        Assert.True(result.Success);
        Assert.Contains("close", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_Open_ShouldReturnSuccessMessage()
    {
        var (sut, _, controller, _) = CreateWithXcbr(simulationRunning: false);
        var registry = GetRegistry(sut);
        SetValue(registry, controller.SwitchDevice.PositionReference, 2);

        var result = sut.Execute(controller, isClose: false);

        Assert.True(result.Success);
        Assert.Contains("open", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Failure paths — AddCause messages ────────────────────────────────

    [Fact]
    public void Execute_WhenLocBlocks_ShouldReturnLocalModeMessage()
    {
        var (sut, registry, controller, _) = CreateWithXcbrLoc();
        SetValue(registry, CswiLocRef, true);

        var result = sut.Execute(controller, isClose: true);

        Assert.False(result.Success);
        Assert.Contains("local mode", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_WhenPositionAlreadyReached_ShouldReturnPositionReachedMessage()
    {
        var (sut, registry, controller, posRef) = CreateWithXcbr(simulationRunning: false);
        SetValue(registry, posRef, 2); // already closed

        var result = sut.Execute(controller, isClose: true); // close again

        Assert.False(result.Success);
        Assert.Contains("position already reached", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private const string BreakerPosRef = "LD/XCBR1.Pos.stVal";
    private const string SwitchPosRef  = "LD/XSWI1.Pos.stVal";
    private const string CswiPosRef    = "LD/CSWI1.Pos.stVal";
    private const string CswiCmdRef    = "LD/CSWI1.Pos.Oper.ctlVal";
    private const string CswiLocRef    = "LD/CSWI1.Loc.stVal";

    private static (ManualOperationService sut, PointRegistry registry, CSWI controller, string posRef)
        CreateWithXcbr(bool simulationRunning = false, bool serverRunning = false)
    {
        var registry = BuildRegistry(BreakerPosRef, "XCBR1", eLnType.XCBR);
        SetValue(registry, BreakerPosRef, 1); // Open
        var xcbr = new XCBR("XCBR1", BreakerPosRef, "Q01", registry);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, xcbr, registry);

        var runtime = MakeRuntime(simulationRunning, serverRunning);
        var sut = new ManualOperationService(new ControlCommandProcessor(registry), runtime);

        return (sut, registry, cswi, BreakerPosRef);
    }

    private static (ManualOperationService sut, PointRegistry registry, CSWI controller, string posRef)
        CreateWithXswi(bool simulationRunning = false)
    {
        var registry = BuildRegistry(SwitchPosRef, "XSWI1", eLnType.XSWI);
        SetValue(registry, SwitchPosRef, 2); // Closed
        var xswi = new XSWI("XSWI1", SwitchPosRef, "Q01", registry, maneuverTime: 0.001);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, xswi, registry);

        var runtime = MakeRuntime(simulationRunning, serverRunning: false);
        var sut = new ManualOperationService(new ControlCommandProcessor(registry), runtime);

        return (sut, registry, cswi, SwitchPosRef);
    }

    private static (ManualOperationService sut, PointRegistry registry, CSWI controller, string posRef)
        CreateWithXcbrLoc()
    {
        var registry = BuildRegistry(BreakerPosRef, "XCBR1", eLnType.XCBR);
        registry.Register(DevicePointFactory.CreatePoint(CswiLocRef, "CSWI1", "Loc", eLnType.CSWI, FunctionalConstraint.ST));
        SetValue(registry, BreakerPosRef, 1);
        SetValue(registry, CswiLocRef, false);

        var xcbr = new XCBR("XCBR1", BreakerPosRef, "Q01", registry);
        var cswi = new CSWI("CSWI1", CswiCmdRef, CswiPosRef, xcbr, registry, locRef: CswiLocRef);

        var runtime = MakeRuntime(simulationRunning: false, serverRunning: false);
        var sut = new ManualOperationService(new ControlCommandProcessor(registry), runtime);

        return (sut, registry, cswi, BreakerPosRef);
    }

    private static PointRegistry BuildRegistry(string posRef, string logicalNode, eLnType lnType)
    {
        var registry = new PointRegistry();
        registry.Register(DevicePointFactory.CreatePoint(posRef, logicalNode, "Pos", lnType, FunctionalConstraint.ST));
        registry.Register(DevicePointFactory.CreatePoint(CswiCmdRef, "CSWI1", "Pos", eLnType.CSWI, FunctionalConstraint.CO));
        registry.Register(DevicePointFactory.CreatePoint(CswiPosRef, "CSWI1", "Pos", eLnType.CSWI, FunctionalConstraint.ST));
        return registry;
    }

    private static IOperationRuntime MakeRuntime(bool simulationRunning, bool serverRunning = false)
    {
        var r = Substitute.For<IOperationRuntime>();
        r.SimulationRunning.Returns(simulationRunning);
        r.ServerRunning.Returns(serverRunning);
        return r;
    }

    // Reflection helpers to access injected runtime/registry from the sut for verification
    private static IOperationRuntime GetRuntime(ManualOperationService sut) =>
        (IOperationRuntime)typeof(ManualOperationService)
            .GetField("_runtime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(sut)!;

    private static PointRegistry GetRegistry(ManualOperationService sut)
    {
        var processor = (ControlCommandProcessor)typeof(ManualOperationService)
            .GetField("_processor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(sut)!;
        return (PointRegistry)typeof(ControlCommandProcessor)
            .GetField("_registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(processor)!;
    }

    private static void SetValue(PointRegistry registry, string reference, object value)
        => registry.SetValue(new PointValue<object>
        {
            Reference = reference,
            Value = value,
            Quality = 192,
            Timestamp = DateTimeOffset.UtcNow
        });
}
