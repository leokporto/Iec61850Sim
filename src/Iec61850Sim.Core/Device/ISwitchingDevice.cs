using IEC61850.Common;

namespace Iec61850Sim.Core.Device;

/// <summary>
/// Abstraction over physical switching elements (XCBR, XSWI).
/// Exposes the operations CSWI and ControlCommandProcessor need without depending on a concrete type.
/// </summary>
public interface ISwitchingDevice
{
    /// <summary>Full registry reference of the Pos.stVal data attribute.</summary>
    string PositionReference { get; }

    /// <summary>Equipment name used to correlate CSWI ↔ physical device in the model.</summary>
    string EquipmentName { get; }

    /// <summary>Full registry reference of the Loc.stVal data attribute, or null when absent.</summary>
    string? LocReference { get; }

    /// <summary>
    /// Commands the device to open.
    /// Returns null on success, or the blocking cause when the command cannot be executed immediately.
    /// Implementations that simulate maneuver time set the position to Intermediate here and
    /// complete the transition asynchronously via <see cref="DeviceBase.Step"/>.
    /// </summary>
    ControlAddCause? Open();

    /// <summary>
    /// Commands the device to close.
    /// Returns null on success, or the blocking cause when the command cannot be executed immediately.
    /// </summary>
    ControlAddCause? Close();
}
