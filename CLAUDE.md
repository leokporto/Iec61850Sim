# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build src/Iec61850Sim.Web/Iec61850Sim.Web.csproj
dotnet build src/Iec61850Sim.Desktop/Iec61850Sim.Desktop.csproj   # Windows only

# Run
dotnet run --project src/Iec61850Sim.Web/Iec61850Sim.Web.csproj

# Tests
dotnet test tests/Iec61850Sim.UnitTests/Iec61850Sim.UnitTests.csproj

# Run a single test class
dotnet test tests/Iec61850Sim.UnitTests/Iec61850Sim.UnitTests.csproj --filter "FullyQualifiedName~DeviceBuilderTests"

# Publish (self-contained, Windows)
dotnet publish src/Iec61850Sim.Web -c Release -r win-x64 --self-contained true

# Docker
docker build -f Docker/Dockerfile -t iec61850sim:latest .
docker run -p 5000:8080 -p 102:102 leokporto/iec61850sim -r
```

Default URLs when running: `http://localhost:8080`. The IEC 61850 MMS server starts on port `102`.

The model file is resolved from the app base directory. Override using the `IEC_MODEL` env var (defaults to `./Config/Demo_Ed2.cfg`).

## Architecture

**Tech stack:** C# .NET 10, ASP.NET Core, Blazor Server, WPF+WebView2 (Windows only), MZ-Automation libiec61850 .NET binding.

**Projects:**

| Project | Purpose |
|---------|---------|
| `src/Iec61850Sim.Core` | Domain logic — no UI, no ASP.NET |
| `src/Iec61850Sim.Web` | Blazor Server app, hosts the IEC server via BackgroundService |
| `src/Iec61850Sim.Desktop` | WPF window with WebView2 pointing to the web app (Windows only) |
| `tests/Iec61850Sim.UnitTests` | xUnit v3 tests for Core only |
| `ThirdPartyRefs/` | `iec61850dotnet.dll` + `iec61850.dll` — **gitignored, must be provided manually** |
| `ConfigFiles/` | IEC 61850 `.cfg`, `.icd`, and `.scl` model files |

**Startup flow (`Program.cs`):**
1. `ConfigFileParser.CreateModelFromConfigFile()` loads the IEC model
2. Core services registered as singletons in DI
3. `ModelScanner.Scan()` traverses the model tree and populates `PointRegistry`
4. `DeviceBuilder.Build()` creates device abstractions from the registry into `DeviceManager`
5. `SimulationService` (BackgroundService) starts the `IecServerHost` and runs the simulation loop

**Core component flow:**

```
ModelScanner → PointRegistry → DeviceBuilder → DeviceManager
                                                     ↓
SimulationEngine.Step(dt) → IecServerHost.Publish()
```

Model traversal: `IedModel → LogicalDevice → LogicalNode → DataObject → DataAttribute`. Each `DataAttribute` becomes a `DevicePoint` in `PointRegistry`.

**Simulated device types:** `MeasurementDevice` (generates values), `Breaker`/`Switch` (maintain state), `Cswi` (control operations).

**Simulation loop** (in `SimulationService`): calls `simulation.Step()` then `iecServerHost.Publish()` every 1000–2000 ms. Must not block the ASP.NET pipeline.

**PointRegistry** is the single source of truth for all device points. `DeviceBuilder` creates device abstractions based on the registry, which are then used by the simulation engine and IEC server.

### Architecture Principles

Use best practices sucha as SOLID, Separation of Concerns, dependency Injection, Low coupling, DRY. 
Prefer vertical slices for organization.

## Key Rules

**Device creation:** Always go through `DeviceBuilder` → `DeviceManager`. Direct instantiation is not allowed.

**libiec61850 access:** Only through proper abstractions (`IecServerHost`). Do not call libiec61850 directly from Web or Desktop layers.

## Skills disponíveis
- `libiec61850` — consultar SEMPRE antes de usar qualquer classe ou método da biblioteca

## Testing

- Test stack: **xUnit v3**, **NSubstitute** (mocking), **Bogus** (test data)
- Only test `Iec61850Sim.Core` — do not test UI, framework config, or external libraries
- TDD: write tests before implementing new public methods
- Naming: class `<ClassName>Tests`, method `<Method>_<Condition>_<ExpectedResult>`
- Mock only external dependencies; never mock domain logic
- Reproduce bugs with a failing test first, then fix

## Workflows

@.claude/rules/WORKFLOWS.md

## Security

@.claude/rules/SECURITY_RULES.md