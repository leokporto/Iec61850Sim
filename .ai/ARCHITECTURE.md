# Architecture — Iec61850Sim

## Tech stack
- C# .NET 10
- ASP.NET Core
- Blazor Server
- WPF (windows only)
- WebView2 (windows only)
- MZ-Automation libiec61850 .NET binding

## High Level Architecture

The system is implemented as a **self-hosted web application** using ASP.NET Core. It also includes a WPF client using WebView2 (windows only).

Architecture type:

Monorepo modular architecture.

Main modules:

- Web Host
- Wpf Client
- Model Loader
- Model Scanner
- Point Registry
- Device Builder
- Device Manager
- Simulation Engine
- IEC Server Host
- Simulation Background Service

Application startup flow:

1. ASP.NET Core Web Host starts
2. IEC model is loaded
3. Services are registered in dependency injection
4. ModelScanner traverses the model
5. DeviceBuilder creates simulated devices
6. IEC server starts
7. Simulation BackgroundService starts
8. Simulation values are periodically published

---

## Repository Structure

```
Iec61850Sim.slnx
README.md
ConfigFiles/
Docker/
src/
    Iec61850Sim.Core/
    Iec61850Sim.Web/
    Iec61850Sim.Desktop/
tests/
    Iec61850Sim.Tests/
ThirdPartyRefs/
```

Descriptions:

| Directory | Description |
|----------|-------------|
| Iec61850Sim.Core | Core simulation logic |
| Iec61850Sim.Web | ASP.NET Core + Blazor Server |
| Iec61850Sim.Desktop | WPF client using WebView2 |
| Iec61850Sim.Tests | Unit tests |
| ConfigFiles | IEC configuration files |
| Docker | Container configuration |
| ThirdPartyRefs | libiec61850 binaries |

---

## Supported Platforms

The simulator must run on:

- Windows
- Linux
- Docker containers