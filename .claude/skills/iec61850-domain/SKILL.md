---
name: iec61850-domain
description: >
  IEC 61850 standard domain knowledge. ALWAYS use when working with
  IEC 61850 concepts, hierarchy, data model, logical nodes, logical devices,
  data objects, data attributes, SCL, MMS, or substation automation terminology.
user-invocable: false
---

# IEC 61850 Domain Knowledge

## Product Model (IED hierarchy)
IED → Server → LDevice → LNode (LN) → DataObject (DO) → DataAttribute (DA)

- **IED**: physical device (relay, controller)
- **Server**: communication entity exposing data via MMS
- **LDevice**: logical grouping within a server; must contain LN0
- **LNode**: fundamental function unit (e.g. PTOC=overcurrent, MMXU=measurement)
- **DataObject (DO)**: specific information within LN; may contain sub-dataobjects (SDOs)
- **DataAttribute (DA)**: leaf level — holds value, quality, timestamp; may be basic (BDA) or structured

## Substation Model (functional hierarchy)
Substation → VoltageLevel → Bay → Equipment/Function → SubEquipment/SubFunction

Describes electrical topology independently of which IED hosts the data.

## SCL DataTypeTemplates
- **LNodeType**: template defining which DOs a LN must contain
- **DOType**: DO structure based on Common Data Classes (CDC)
- **DAType**: composite DA structure
- **EnumType**: enumerated value lists

## Signal identification (pathname or Reference)
`LDName / LNPrefix + LNClass + LNInst . DataName . DataAttributeName`

Example: `DemoMeasurement/MMXU1.PhV.phsA.cVal.mag`

## Key concepts
- Hierarchy ensures interoperability across vendors via SCL
- CDC (Common Data Classes) standardize DO structures

## Common Logical Nodes (LNode or LN)
- XCBR — Circuit Breaker
- XSWI — Switch
- MMXU — Measurements (voltage, current, power)
- CSWI — Switch Controller
- PTOC — Overcurrent Protection
- LN0 — Logical Node Zero (device management)
