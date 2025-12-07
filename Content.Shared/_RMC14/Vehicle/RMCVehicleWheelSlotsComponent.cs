using Content.Shared.Containers.ItemSlots;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

/// <summary>
/// Marker placed on wheel items so they can be inserted into wheel slots.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleWheelSystem))]
public sealed partial class RMCVehicleWheelItemComponent : Component;

/// <summary>
/// Adds wheel slots to a vehicle and drives related visuals and logic.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleWheelSystem))]
public sealed partial class RMCVehicleWheelSlotsComponent : Component
{
    /// <summary>
    /// Component id that wheel items must have to be accepted into wheel slots.
    /// </summary>
    public const string WheelComponentId = "RMCVehicleWheelItem";

    /// <summary>
    /// Number of wheel slots to add if none are explicitly provided.
    /// </summary>
    [DataField]
    public int SlotCount = 4;

    /// <summary>
    /// Optional explicit wheel slot ids. If empty, ids are generated with <see cref="SlotPrefix"/>.
    /// </summary>
    [DataField]
    public List<string> Slots = new();

    /// <summary>
    /// Prefix used when generating default slot ids.
    /// </summary>
    [DataField]
    public string SlotPrefix = "wheel";

    /// <summary>
    /// Whitelist for items that can be inserted into wheel slots.
    /// </summary>
    [DataField]
    public EntityWhitelist WheelWhitelist = new()
    {
        Components = new[] { WheelComponentId },
    };
}

[Serializable, NetSerializable]
public enum RMCVehicleWheelVisuals : byte
{
    HasAllWheels,
    WheelCount,
}

public static class RMCVehicleWheelLayers
{
    public const string Wheels = "rmc-wheels";
}
