# IEC 61850 Domain

.NET binding of libiec61850:
- `IedModel` is NOT a `ModelNode`
- `LogicalDevice`/`LogicalNode` expose `GetChildren()`
- Get logical devices via `GetDeviceByInst()`
- Register control handlers via `GetModelNodeByShortObjectReference()`
- Use dot notation only — never MMS-style (`$MX$`, `$ST$`)

Object ref format: `LogicalDevice/LogicalNode.DataObject.SubObject.Attribute`
Example: `DemoMeasurement/U3pMMXU2.PhV.phsA.cVal.mag`

Devices: XCBR (Breaker), XSWI (Switch), MMXU (Measurement), CSWI (Controller)
Nested DA structure: value attributes (e.g. `f`) live inside structured DAs (`cVal → mag → f`), not as direct DataObject children. When traversing the model tree, recurse into non-leaf DAs to reach leaf value attributes.