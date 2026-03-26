# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build src/Iec61850Sim.Web/Iec61850Sim.Web.csproj
dotnet build src/Iec61850Sim.Desktop/Iec61850Sim.Desktop.csproj   # Windows only

# Run
dotnet run --project src/Iec61850Sim.Web/Iec61850Sim.Web.csproj

# Tests — xUnit v3 requires dotnet run, NOT dotnet test
dotnet run --project tests/Iec61850Sim.UnitTests/Iec61850Sim.UnitTests.csproj

# Run a single test class
dotnet run --project tests/Iec61850Sim.UnitTests/Iec61850Sim.UnitTests.csproj -- --filter "FullyQualifiedName~DeviceBuilderTests"

# Publish (self-contained, Windows)
dotnet publish src/Iec61850Sim.Web -c Release -r win-x64 --self-contained true

# Docker
docker build -f Docker/Dockerfile -t iec61850sim:latest .
docker run -p 5000:8080 -p 102:102 leokporto/iec61850sim -r
```

Default URLs when running: `http://localhost:8080`. The IEC 61850 MMS server starts on port `102`.

The model file is resolved from the app base directory. Override using the `IEC_MODEL` env var (defaults to `./Config/Demo_Ed2.cfg`).

## Architecture

**Tech stack:** C# .NET 10, ASP.NET Core, Blazor Server, Mudblazor, WPF+WebView2 (Windows only), MZ-Automation libiec61850 .NET binding.

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
                    ↓                               ↓
             ModelTreeBuilder           SimulationEngine.Step(dt) → IecServerHost.Publish()
                    ↓
              IModelNode tree (UI)
```

Model traversal: `IedModel → LogicalDevice → LogicalNode → DataObject → DataAttribute`. Each `DataAttribute` becomes a `DevicePoint` in `PointRegistry`.

**Simulated device types:** `MeasurementDevice` (generates values), `Breaker`/`Switch` (maintain state), `Cswi` (control operations).

**Simulation loop** (in `SimulationService`): calls `simulation.Step()` then `iecServerHost.Publish()` every 1000–2000 ms. Must not block the ASP.NET pipeline.

**`SimulationEngine.ReRegisterDevices()`** — clears the `SimulationClock` task list and re-registers all current devices from `DeviceManager`. Must be called after a server restart that repopulates `DeviceManager`.

**`IedServerManager`** (singleton, `Iec61850Sim.Web.Services`) — orchestrates safe stop/restart of the IEC server. Exposes `RestartAsync(modelPath, serverModel, port)`. Must be singleton (not scoped) because the IEC server is application-level state shared across all Blazor circuits. Guards concurrent restarts with `volatile bool _isRestarting`.

**`IecServerHost.Reinitialize(IedModel, string serverModel)`** — recreates the internal `IedServer` with a new model and re-registers control handlers. Does **not** call `Start()` — caller must invoke `RuntimeEngine.StartServer(port)` afterward.

**`RuntimeEngine.StartServer(int port = 102)`** — starts the IEC server on the given port (default 102 for backward compatibility), sets `ServerRunning = true`, and initializes static device state.

**PointRegistry** is the single source of truth for all device point values.
- `Register(DevicePoint)` — adds a point (called by `ModelScanner`)
- `Clear()` — removes all registered points (used during server restart)
- `GetPoint(reference)` — structural lookup by full DA object reference
- `GetValue<T>(reference)` / `SetValue<T>(PointValue<T>)` — typed single value access
- `GetValues(refs)` / `SetValues(values)` — batch value access
- `SetValue` mutates `DevicePoint.Value/Quality/Timestamp` in-place; devices must NEVER write those fields directly

**`PointValue<T>`** — mutable DTO used as the registry read/write currency. `Reference` and `FC` are `init`-only (set at build time). `Value`, `Quality` (`ushort`, default 192 = Good), and `Timestamp` are mutable and refreshed from the registry on each `GetLivePoints` call.

**`IModelNode` tree** (built by `ModelTreeBuilder`):
- `GetPoints()` — returns `IEnumerable<PointValue<object>>` (static, build-time snapshot)
- `GetLivePoints(registry)` — refreshes `Value/Quality/Timestamp` from registry and returns live values
- `DataAttributeNode` is the leaf; stores a `PointValue<object>` with `Reference`+`FC` set at build time
- **Important:** In real IEC 61850 models, value DataAttributes (e.g., `f`) are nested inside structured DAs (`cVal → mag → f`), NOT as direct children of the DataObject. `ModelTreeBuilder.CollectLeafPoints` recurses through nested DAs to reach them — do not flatten this traversal.

**Settings page** (`/settings`): MudBlazor form to change `ModelPath`, `ServerModel`, and `Port`. "Apply & Restart" calls `IedServerManager.RestartAsync()`. Note: changing `ModelPath` to a different `.cfg` requires a browser page reload to see the updated `IModelNode` tree (it is a DI singleton built at startup and not replaced on restart).

**Mudblazor reference** - https://www.mudblazor.com/docs/overview

### Architecture Principles

Use best practices sucha as SOLID, Separation of Concerns, dependency Injection, Low coupling, DRY.
Prefer vertical slices for organization.

## Key Rules

**Device creation:** Always go through `DeviceBuilder` → `DeviceManager`. Direct instantiation is not allowed.

**libiec61850 access:** Only through proper abstractions (`IecServerHost`). Do not call libiec61850 directly from Web or Desktop layers.

**Value writes:** Always via `registry.SetValue<T>` or `registry.SetValues`. Never mutate `DevicePoint.Value`, `DevicePoint.Quality`, or `DevicePoint.Timestamp` directly.

**Model tree traversal:** `ModelTreeBuilder.CollectLeafPoints` must recurse through nested DataAttributes to reach actual leaf DAs. In real models, value attributes like `f` live inside `cVal → mag → f` (all DataAttributes), not as direct DataObject children. Direct-children-only iteration will only find description (`d`) attributes.

## Skills disponíveis
- `libiec61850` — consultar SEMPRE antes de usar qualquer classe ou método da biblioteca

## Testing

- Test stack: **xUnit v3**, **NSubstitute** (mocking), **Bogus** (test data)
- Only test `Iec61850Sim.Core` — do not test UI, framework config, or external libraries
- TDD: write tests before implementing new public methods
- Naming: class `<ClassName>Tests`, method `<Method>_<Condition>_<ExpectedResult>`
- Mock only external dependencies; never mock domain logic
- Reproduce bugs with a failing test first, then fix
