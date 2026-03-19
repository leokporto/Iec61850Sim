# Simulation

BackgroundService loop (1000–2000ms): `simulation.Step()` → `iecServerHost.Publish()`

Components: `SimulationClock` (time), `SimulationEngine` (updates devices)
Devices: `MeasurementDevice` (values), `Breaker`/`Switch` (state), `CSWI` (control)
Value writes: devices store reference strings (not DevicePoint refs) and write via `registry.SetValue`/`registry.SetValues` — never mutate DevicePoint fields directly.