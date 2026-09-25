namespace Iec61850Sim.Core.Comtrade.Models;

/// <summary>Tipo de perturbação sintetizada no registro COMTRADE.</summary>
public enum ComtradeScenario
{
    /// <summary>Falta fase A-terra.</summary>
    SinglePhaseToGround,

    /// <summary>Falta bifásica B-C.</summary>
    PhaseToPhase,

    /// <summary>Falta trifásica.</summary>
    ThreePhase,

    /// <summary>
    /// Falta A-terra com religamento sem sucesso:
    /// falta → abertura (tempo morto) → religamento sob falta → abertura definitiva.
    /// </summary>
    AutoReclose
}
