using Iec61850Sim.Api.Model.Devices;

namespace Iec61850Sim.Api.Core.Model;

public class DeviceBinder
{
    /*
    public void Bind(List<Cswi> controllers, List<Breaker> breakers)
    {
        foreach (var cswi in controllers)
        {
            var prefix = ExtractPrefix(cswi.Name);

            var breaker = breakers
                .FirstOrDefault(b => b.Name.StartsWith(prefix));

            if (breaker != null)
                cswi.BindBreaker(breaker);
        }
    }*/
}