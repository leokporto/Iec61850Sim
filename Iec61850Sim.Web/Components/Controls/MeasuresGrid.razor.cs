using Iec61850Sim.Web.Model;
using Iec61850Sim.Web.Model.Devices;
using Microsoft.AspNetCore.Components;

namespace Iec61850Sim.Web.Components.Controls;

public partial class MeasuresGrid : ComponentBase
{
    
    public IEnumerable<DevicePoint> Measures { get; set; }
    
    
}