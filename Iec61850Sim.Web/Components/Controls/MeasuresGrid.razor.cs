using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Microsoft.AspNetCore.Components;

namespace Iec61850Sim.Web.Components.Controls;

public partial class MeasuresGrid : ComponentBase, IDisposable
{

    private List<DevicePoint> Measures { get; set; } = new();
    private PeriodicTimer? timer;
    private CancellationTokenSource cts = new();
    private string? _lastLogicalDevice;
    private string? _lastLogicalNode;

    [Parameter]
    public string? LogicalDevice { get; set; }

    [Parameter]
    public string? LogicalNode { get; set; }
    

    protected override void OnInitialized()
    {
        Measures = GetMeasures();

        timer = new PeriodicTimer(TimeSpan.FromMilliseconds(2000));

        _ = RefreshLoop(cts.Token);
    }

    protected override void OnParametersSet()
    {
        if (_lastLogicalDevice != LogicalDevice ||
            _lastLogicalNode != LogicalNode)
        {
            _lastLogicalDevice = LogicalDevice;
            _lastLogicalNode = LogicalNode;

            Measures = GetMeasures();
        }
    }
    
    private async Task RefreshLoop(CancellationToken token)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                Measures = GetMeasures();

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    
    private List<DevicePoint> GetMeasures()
    {
        IEnumerable<DevicePoint> query = registry.All;

        query = query.Where(x => x.LnType == eLnType.MMXU);

        if (!string.IsNullOrWhiteSpace(LogicalDevice))
            query = query.Where(x => x.LogicalDevice == LogicalDevice);

        if (!string.IsNullOrWhiteSpace(LogicalNode))
            query = query.Where(x => x.LogicalNode == LogicalNode);

        return query
            .Select(x => new DevicePoint
            {
                Reference = x.Reference,
                LogicalNode = x.LogicalNode,
                DataObject = x.DataObject,
                Value = x.Value,
                Timestamp = x.Timestamp
            })
            .ToList();
    }

    public void Dispose()
    {
        cts.Cancel();
        timer?.Dispose();
    }


}