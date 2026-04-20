using Iec61850Sim.Core.Comtrade.Models;

namespace Iec61850Sim.Core.Comtrade;

public interface IComtradeService
{
    Task<ComtradeRecord> GenerateFaultAsync(ComtradeOptions options);
    IReadOnlyList<ComtradeRecord> GetGeneratedRecords();
}