using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vehicle;

/// <summary>
/// Placed on the exterior of an RMC vehicle. Handles loading the interior map when used.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleSystem))]
public sealed partial class VehicleEnterComponent : Component
{
    /// <summary>
    /// Path to the interior map that should be loaded for this vehicle.
    /// </summary>
    [DataField(required: true)]
    public ResPath InteriorPath;

    /// <summary>
    /// Offset from the vehicle's position where users will be placed when exiting.
    /// Local to the vehicle's rotation.
    /// </summary>
    [DataField]
    public Vector2 ExitOffset = Vector2.Zero;
}

/// <summary>
/// Marker for an interior exit. Interacting with this entity should send you back outside.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleSystem))]
public sealed partial class VehicleExitComponent : Component;

/// <summary>
/// A pilot seat inside a vehicle interior. Buckling here lets you operate the linked vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleSystem))]
public sealed partial class VehicleDriverSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills = new();
}
