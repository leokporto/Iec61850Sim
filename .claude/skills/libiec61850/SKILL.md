---
name: libiec61850
description: >
  Consult the libiec61850 .NET binding API (MZ-Automation).
  ALWAYS use before writing any code involving IedServer,
  IedModel, LogicalDevice, LogicalNode, DataAttribute or IedServerConfig.
  Never invent methods — consult this skill first.
user-invocable: false
---

# libiec61850 .NET Binding

Before using any class or method from this library, consult the official documentation:

- .NET API: https://support.mz-automation.de/doc/libiec61850/net/latest/
- .NET binding source: https://github.com/mz-automation/libiec61850/tree/v1.6/dotnet
- SCL Model generator: https://github.com/mz-automation/libiec61850/tree/v1.6/tools/model_generator_dotnet
- API Overview: https://libiec61850.com/documentation/

Critical rules:
- `IedModel` is NOT a `ModelNode`
- `LogicalDevice`/`LogicalNode` expose `GetChildren()`
- Get logical devices via `GetDeviceByInst()`
- Register control handlers via `GetModelNodeByShortObjectReference()`
- Dot notation only — never MMS-style (`$MX$`, `$ST$`)
- Never assume parity with the C API