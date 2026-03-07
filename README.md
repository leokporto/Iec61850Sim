# IEC 61850 Simulator (Iec61850Sim)

A small **self-hosted web application** and **IEC 61850 server simulator** implemented in **C# / ASP.NET Core (.NET 10)** using the `iec61850dotnet` library.

The application loads an IEC 61850 model from configuration files, starts an IEC 61850 MMS server, builds and binds simulated devices to model points, and continuously publishes simulated values in the background.

This project is useful for:

- Testing IEC 61850 client applications against a simulated server
- Validating IEC 61850 model/configuration files
- Running a lightweight self-hosted simulator for development and integration scenarios
- Experimenting with simulated device behavior and periodic value publication

---

## 🚀 Quick Start

### 1) Prerequisites

- .NET 10 SDK
- MZ-Automation [IEC 61850](https://github.com/mz-automation/libiec61850) libraries
- Iec 61850 cfg file (created by jar package)
- Iec 61850 icd file

### 2) Build

From the repository root:

```bash
dotnet build Iec61850Sim.Api/Iec61850Sim.Api.csproj
``` 

### 3) Run

```bash
dotnet run --project Iec61850Sim.Api/Iec61850Sim.Api.csproj
```


By default, the web application runs using the ASP.NET Core launch settings:

- HTTP: `http://localhost:5056`
- HTTPS: `https://localhost:7242`

At startup, the application also starts the **IEC 61850 server** on port **102**.

To stop the application, press **Ctrl+C**.

---

## 🏗️ Application Model

This project should be treated as a **self-hosted web application**, not a console application.

The runtime behavior is centered on the `Iec61850Sim.Api` project:

- ASP.NET Core hosts the application
- A background hosted service runs the simulation loop
- The IEC 61850 server is started during application startup
- Model points are scanned, registered, bound to devices, and periodically published

OpenAPI support is enabled and mapped in the **Development** environment.

---

## ⚙️ Configuration

The simulator loads the IEC 61850 model from a config file called `Demo_Ed2.cfg` by default.

The IEC 61850 model and related files are stored under:

- `Iec61850Sim.Api/Config/Demo_Ed2.cfg`
- `Iec61850Sim.Api/Config/Demo_Ed2.icd`
- `Iec61850Sim.Api/Config/rfc1006.cfg`

These files are copied to the output directory during build.

By default, the application loads:

- `Demo_Ed2.cfg`

The model file is resolved from the application base directory at runtime.

If you want to use a different config file:

Update the filename passed to ConfigFileParser.CreateModelFromConfigFile(...) in Program.cs.
Ensure the .cfg file is available in the working directory when running the app.
The companion .icd file (Demo_Ed2.icd) is included for tools that need it.

You can also use SCL files and convert them to .cfg using getconfig.jar tool in "Modelos" folder using this command prompt:

`java -jar getconfig.jar Demo_Ed2.scl Demo_Ed2.cfg`

The application sets the IED name to:

- `Demo`

---

## 🔄 Simulation Behavior

When the application starts, it performs the following high-level flow:

1. Creates the ASP.NET Core web host
2. Loads the IEC 61850 model from `Demo_Ed2.cfg`
3. Registers core services in dependency injection
4. Scans the model for relevant points
5. Builds device abstractions from the discovered points
6. Starts a hosted background service
7. Starts the IEC 61850 server on port `102`
8. Periodically advances the simulation and publishes updated values

The simulation loop runs continuously in the background with a timed delay between publish cycles.

---

## 🧩 Main Components

- Web hosting
- IEC 61850 server
- Model scanning and binding
- Device simulation
- Background simulation loop
- Configuration file loading
- OpenAPI integration


## 🌐 Web Hosting Notes

This application uses:

- `Microsoft.NET.Sdk.Web`
- ASP.NET Core hosting model
- Dependency Injection
- Hosted background services
- OpenAPI integration in Development

Although the simulator’s core job is to host an IEC 61850 server, it now runs inside a web application host, which makes it easier to extend with HTTP APIs, diagnostics, health checks, or operational endpoints later.

A nice little upgrade from “just runs in a console” to “lives like a proper service.”

---

## 📝 Notes

- This is a lightweight simulator intended for testing and experimentation
- It is not a complete IEC 61850 platform implementation
- The current implementation source of truth is `Iec61850Sim.Api`
- Older console-oriented documentation should no longer be considered deprecated

---

## 🧩 License

This project is licensed under the GNU GPL v3 license.

This project uses the library libiec61850:
https://github.com/mz-automation/libiec61850

libiec61850 is licensed under the GNU GPL v3.
