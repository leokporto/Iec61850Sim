# Workflows

## Standard Flow
1. Understand task → check repo for existing code → identify affected components
2. Implement following patterns → write tests (TDD) → validate against rules → run all tests
3. Document if necessary

## Rules
- Check existing implementations before creating new ones; extend over create
- Never bypass DeviceBuilder or DeviceManager
- Never access libiec61850 directly outside abstractions
- Simulation: fit SimulationEngine, never block BackgroundService, keep loop deterministic
- IEC 61850: use correct object references, never invent attributes, validate against model
- Value writes: always via `registry.SetValue` / `registry.SetValues`; never mutate `DevicePoint.Value/Quality/Timestamp` directly
- Bugs: identify root cause → failing test → fix → validate