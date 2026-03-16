using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Model.Devices;
using Microsoft.AspNetCore.Components;

namespace Iec61850Sim.Web.Components.Controls;

public partial class XcbrCswiStatesGrid : ComponentBase, IDisposable
{
    private const string ON_STATE_CSS_CLASS = "text-danger";
    private const string OFF_STATE_CSS_CLASS = "text-success";

    private List<PointStateRow> Rows { get; set; } = [];
    private PeriodicTimer? _timer;
    private readonly CancellationTokenSource _cts = new();
    private string? _lastLogicalDevice;
    private string? _lastLogicalNode;

    [Parameter]
    public string? LogicalDevice { get; set; }

    [Parameter]
    public string? LogicalNode { get; set; }

    protected override void OnInitialized()
    {
        Rows = GetRows();

        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(2000));

        _ = RefreshLoop(_cts.Token);
    }

    protected override void OnParametersSet()
    {
        if (_lastLogicalDevice != LogicalDevice ||
            _lastLogicalNode != LogicalNode)
        {
            _lastLogicalDevice = LogicalDevice;
            _lastLogicalNode = LogicalNode;

            Rows = GetRows();
        }
    }

    private async Task RefreshLoop(CancellationToken token)
    {
        if (_timer is null)
            return;

        try
        {
            while (await _timer.WaitForNextTickAsync(token))
            {
                Rows = GetRows();

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private List<PointStateRow> GetRows()
    {
        IEnumerable<Cswi> query = deviceManager.Controllers;

        if (!string.IsNullOrWhiteSpace(LogicalDevice))
        {
            query = query.Where(x =>
                string.Equals(x.Position.LogicalDevice, LogicalDevice, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Breaker.Position.LogicalDevice, LogicalDevice, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(LogicalNode))
        {
            query = query.Where(x =>
                string.Equals(x.Position.LogicalNode, LogicalNode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Breaker.Position.LogicalNode, LogicalNode, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .SelectMany(x =>
            {
                PointState cswiState = GetPointState(x.Position);
                PointState xcbrState = GetPointState(x.Breaker.Position);

                return new[]
                {
                    new PointStateRow($"CSWI:{x.Position.Reference}", "CSWI", x.Position.Reference, cswiState.Text, cswiState.CssClass),
                    new PointStateRow($"XCBR:{x.Breaker.Position.Reference}", "XCBR", x.Breaker.Position.Reference, xcbrState.Text, xcbrState.CssClass)
                };
            })
            .ToList();
    }

    private static PointState GetPointState(DevicePoint point)
    {
        if (TryGetDblPos(point.Value, out eDblPos state))
        {
            return state switch
            {
                eDblPos.Off => new PointState("Off (Open)", OFF_STATE_CSS_CLASS),
                eDblPos.On => new PointState("On (Closed)", ON_STATE_CSS_CLASS),
                eDblPos.Intermediate => new PointState("Intermediate", string.Empty),
                eDblPos.BadState => new PointState("Bad state", string.Empty),
                _ => new PointState("Unknown", string.Empty)
            };
        }

        return new PointState("Unknown", string.Empty);
    }

    private static bool TryGetDblPos(object? value, out eDblPos state)
    {
        switch (value)
        {
            case eDblPos dblPos:
                state = dblPos;
                return true;

            case int intValue when Enum.IsDefined(typeof(eDblPos), intValue):
                state = (eDblPos)intValue;
                return true;

            case long longValue when Enum.IsDefined(typeof(eDblPos), (int)longValue):
                state = (eDblPos)longValue;
                return true;

            default:
                state = default;
                return false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer?.Dispose();
        _cts.Dispose();
    }

    private sealed record PointStateRow(
        string RowKey,
        string PointType,
        string Reference,
        string StateText,
        string StateCssClass);

    private sealed record PointState(
        string Text,
        string CssClass);
}
