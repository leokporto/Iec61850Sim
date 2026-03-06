# IEC 61850 Simulator (Iec61850Sim)

A small **IEC 61850 Server simulator** implemented in C# (.NET 8) and using the [Mz-Automation library](https://github.com/mz-automation/libiec61850). The extension loads an IEC 61850 configuration (`.cfg`/`.icd`) file, exposes the data model over RFC1006/MMS, and updates a set of configured analog/digital points periodically.

This project is useful for:

- Testing IEC 61850 client applications (GOOSE, MMS, reporting) against a simple server simulator
- Validating configuration files (`.cfg`, `.icd`) and data model structures
- Demonstrating control command handling (SPC/CO) and report/event callbacks

---

## 🚀 Quick Start

### 1) Prerequisites

- .NET 8 SDK (tested on .NET 8)

### 2) Build & Run

From the repository root:

```bash
dotnet build Iec61850Sim/Iec61850Sim.csproj
dotnet run --project Iec61850Sim/Iec61850Sim.csproj
```

The server listens on port **102** (IEC 61850 MMS). To stop the simulator, press **Ctrl+C**.

---

## ⚙️ Configuration

The simulator loads the IEC 61850 model from a config file called `Demo_Ed2.cfg` by default (located in `Iec61850Sim/` and also copied to `bin/Debug/net8.0/`).

If you want to use a different config file:

1. Update the filename passed to `ConfigFileParser.CreateModelFromConfigFile(...)` in `Program.cs`.
2. Ensure the `.cfg` file is available in the working directory when running the app.

The companion `.icd` file (`Demo_Ed2.icd`) is included for tools that need it.

You can also use SCL files and convert them to `.cfg` using getconfig.jar tool in "Modelos" folder using this command prompt:

```bash
java -jar getconfig.jar Demo_Ed2.scl Demo_Ed2.cfg
```

---

## 🔧 Customizing Simulation Points

The set of points updated on each loop is defined in `Iec61850Sim/Program.cs` inside `Iec61850ModelHelper.LoadVariables()`.

- **Analog points (floating)** are updated in `UpdateMeasures(...)`.
- **Digital points (integer/states)** are initialized in `InitializeDigitalPoints(...)`.

To add/remove points, update the `AnalogicPoints.Add(...)` and `DigitalPoints.Add(...)` lines.

---

## 🧠 Behavior & Supported Features

- Loads an IEC 61850 model from `.cfg` and sets the IED name to `Demo`
- Handles binary control commands (SPC/CO) and logs command parameters
- Supports select/unselect and report control block (RCB) event callbacks
- Periodically updates configured analog and digital data points

---

## 📦 Project Structure

- `Iec61850Sim/Program.cs` – main application logic and simulation loop
- `Iec61850Sim/Models/Variable.cs` – model classes for analog/digital points
- `Demo_Ed2.cfg`, `Demo_Ed2.icd` – sample IEC 61850 configuration files

---

## 📝 Notes

- This is a lightweight simulation intended for testing and experimentation. It is not a full IEC 61850 stack implementation.

---

## 🧩 License

(Include your preferred license information here.)
