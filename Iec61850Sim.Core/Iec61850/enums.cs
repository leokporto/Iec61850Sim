namespace Iec61850Sim.Core.Iec61850;

public enum eLnType
{
    Unknown,

    MMXU,
    XCBR,
    XSWI,
    CSWI,

    GGIO,

    PTOC,
    PTRC,
    PDIS,

    CILO,

    LLN0,
    LPHD
}

public enum eDblPos
{
    Intermediate = 0,
    Off = 1,
    On = 2,
    BadState = 3
}