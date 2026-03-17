# Simulation

BackgroundService loop (1000–2000ms): `simulation.Step()` → `iecServerHost.Publish()`

Components: `SimulationClock` (time), `SimulationEngine` (updates devices)
Devices: `MeasurementDevice` (values), `Breaker`/`Switch` (state), `CSWI` (control)