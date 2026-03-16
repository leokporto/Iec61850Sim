# Coding Patterns

## Device Creation Pattern

Devices are created using DeviceBuilder.

Flow:

PointRegistry
→ DeviceBuilder
→ DeviceManager

Direct creation of devices outside DeviceBuilder should be avoided.

---

## Device Manager Pattern

DeviceManager acts as the central registry of devices.

Device types:

- Breakers
- Switches
- Measurements
- Controllers

---

## Simulation Pattern

Devices implement simulation behavior through simulation steps.

Expected method:

Step(double dt)

Measurement devices generate simulated values.

Switches and breakers maintain state.

---

## Model Scanning Pattern

ModelScanner traverses the IED model.

Traversal structure:

IedModel
→ LogicalDevice
→ LogicalNode
→ DataObject
→ DataAttribute

Each discovered attribute becomes a DevicePoint.

DevicePoints are stored in PointRegistry.

---

### IEC 61850 Naming

When referring to IEC attributes, preserve the original naming.

Examples:

```
Pos.stVal
PhV.phsA.cVal.mag
CSWI.Pos.Oper.ctlVal
```

---

## Naming Conventions (C#)

The project follows the official C# naming conventions defined by Microsoft.

General rule:

- Use **PascalCase** for types and public members.
- Use **camelCase** for local variables and parameters.
- Prefix **private fields** with `_`.

Avoid abbreviations unless they are widely known.

### Classes / Structs / Records

Use **PascalCase** and nouns.

Examples:

```
class DeviceManager
class SimulationEngine
class MeasurementDevice
```

### Interfaces

Use **PascalCase** and prefix with `I`.

Examples:

```
interface IDevice
interface ISimulationService
interface IPointRegistry
```

### Methods

Use **PascalCase** and verbs or verb phrases.

Examples:

```
StartServer()
PublishValues()
LoadModel()
ScanModel()
StepSimulation()
```

### Properties

Use **PascalCase** and noun phrases.

Examples:

```
public string Reference { get; set; }
public double Value { get; set; }
public bool IsConnected { get; set; }
```

Properties represent data and therefore should not be verbs.

### Method Parameters

Use **camelCase**.

Examples:

```
public void Step(double deltaTime)
public void Publish(DevicePoint point)
```

### Local Variables

Use **camelCase**.

Examples:

```
var devicePoints = registry.GetAll();
var simulationTime = clock.Now;
```

### Private Fields

Use **_camelCase** (underscore prefix).

Examples:

```
private readonly PointRegistry _registry;
private readonly SimulationClock _clock;
private readonly IedServer _server;
```

### Namespaces

Use **PascalCase** and reflect project hierarchy.

Examples:

```
Iec61850Sim.Core
Iec61850Sim.Simulation
Iec61850Sim.Devices
```

### Collections

Collection variables should use **plural nouns**.

Examples:

```
devices
breakers
measurements
points
```

### General Naming Guidelines

- Prefer **descriptive names**.
- Avoid abbreviations unless widely recognized.
- Do not include type names in identifiers.
- Write XML comments on all public or protected classes, methods and properties

Example:
```
GetLength()   ✔
GetInt()      ✘
```

### Project Specific Naming Rules

**Constants** - must be written in UPPER_CASE_WITH_UNDERSCORES.

Example:

```
MAX_SIMULATION_DEVICES
```

**Enums** - must start with prefix `e`.

Example:

```
eDeviceType
eSimulationState
```

Private class variables must use `_camelCase`.

Example:

```
private DeviceManager _deviceManager;
```

