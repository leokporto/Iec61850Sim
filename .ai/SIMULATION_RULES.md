# Simulation Rules

Simulation runs inside an ASP.NET Core BackgroundService.

Simulation loop:

simulation.Step()
iecServerHost.Publish()

Loop interval:

1000–2000 ms

---

## Simulation Engine Components

- SimulationClock
- SimulationEngine

Responsibilities:

SimulationEngine updates device states.

SimulationClock provides time progression.

---

## Device Simulation Behavior

MeasurementDevice

- generates simulated measurement values

Breaker / Switch

- maintain state

CSWI

- controller responsible for switching operations

---
