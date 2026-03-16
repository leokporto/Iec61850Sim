# Agent Context — Iec61850Sim

You are working on the project **Iec61850Sim**.

Official repository:
https://github.com/leokporto/Iec61850Sim

This project is an IEC 61850 simulator implemented as a self-hosted web application using ASP.NET Core and the .NET binding of the libiec61850 library from MZ-Automation. It has also a WPF client using WebView2 (windows only).

The simulator must:

- Load an IEC 61850 model from a CFG file using ConfigFileParser for the first version. In the future, support for ICD files and dynamic model loading may be added.
- Create an IedServer.
- Simulate measurements and device states.
- Allow SCADA clients to connect and interact with the simulated IED.
- Run entirely inside an ASP.NET Core application using BackgroundServices.

Primary references:

- https://github.com/leokporto/Iec61850Sim
- https://github.com/mz-automation/libiec61850/tree/v1.6/dotnet
- https://github.com/mz-automation/libiec61850/tree/v1.6/tools/model_generator_dotnet
- https://libiec61850.com/documentation/
- https://libiec61850.com/documentation/using-the-c-api/
- https://support.mz-automation.de/doc/libiec61850/net/latest/

Critical rules:

1. Always rely on the actual .NET binding API of libiec61850.
2. Never invent methods or properties.
3. Prefer minimal and robust designs.
4. Always check the project repository before writing code.
5. Never assume the C API is identical to the .NET binding.

The project is organized as a modular monorepo.

Main components include:

- ModelScanner
- PointRegistry
- DeviceBuilder
- DeviceManager
- SimulationEngine
- SimulationService
- IEC Server Host

The simulator runs as a **self-hosted ASP.NET Core application** and a **WPF client** (windows only).

Simulation runs in a BackgroundService and must not block the ASP.NET pipeline.

# Common commands


## Web Host

The Web Host (Blazor server project) uses .NET 10. Run all commands in the `src/Iec61850Sim.Web` directory:

```bash
dotnet build Iec61850Sim.Web/Iec61850Sim.Web.csproj             # Build the web project
dotnet publish -c Release -r win-x64 --self-contained true      # Publish a self-contained version of the web project
dotnet run --project Iec61850Sim.Web/Iec61850Sim.Web.csproj     # Run the web project
```

## WPF Client (windows only)

The WPF client uses WebView2 to host the web application. Run all commands in the `src/Iec61850Sim.Desktop` directory:

```bash
dotnet build Iec61850Sim.Desktop/Iec61850Sim.Desktop.csproj             # Build the WPF client project
dotnet publish -c Release -r win-x64 --self-contained true              # Publish a self-contained version of the WPF client
dotnet run --project Iec61850Sim.Desktop/Iec61850Sim.Desktop.csproj     # Run the WPF client project
```

## Docker

To build and run the application using Docker, execute the following commands from the repository root:

```bash
docker build -f Docker/Dockerfile -t iec61850sim:latest .      # Build the Docker image
docker run -p 5000:8080 -p 102:102 leokporto/iec61850sim -r    # Run the Docker container
docker push leokporto/iec61850sim:latest                       # Push the Docker image to the registry
```

# Workflows

## Creating/Modifying features

When creating or modifying features, follow this workflow:

1. Check the official project repository for existing code and documentation related to the feature you want to implement or modify. Always rely on the actual code and documentation in the repository.
2. Confirm proposed changes with user
3. Implement the feature following the project's coding patterns and architecture principles.
4. Write unit tests for the new or modified code.
5. Run all tests to ensure nothing is broken.
6. Document the feature in the official documentation if necessary.
7. Resume the changes and add them to CHANGELOG.md if applicable.

