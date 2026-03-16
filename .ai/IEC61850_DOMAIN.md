# IEC 61850 Domain Notes

The simulator relies on the .NET binding of libiec61850.

Important library characteristics:

- IedModel is NOT a ModelNode.
- LogicalDevice and LogicalNode expose GetChildren().
- LogicalDevices must be obtained using GetDeviceByInst().

Object references follow this format:

LogicalDevice/LogicalNode.DataObject.SubObject.Attribute

Example:

DemoMeasurement/U3pMMXU2.PhV.phsA.cVal.mag

Control handlers must be registered using:

GetModelNodeByShortObjectReference()

Never assume MMS-style references using:

$MX$
$ST$

The .NET API uses dot notation.

---

## Supported Logical Devices

Examples of simulated devices:

- XCBR (Breaker)
- XSWI (Switch)
- MMXU (Measurement)
- CSWI (Controller)