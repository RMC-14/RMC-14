using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vehicle;

[DataDefinition]
public sealed partial class VehicleEntryPoint
{
    [DataField(required: true)]
    public Vector2 Offset;

    [DataField]
    public float Radius = 0.6f;

    /// <summary>
    /// Optional local interior coordinates to visualize where this entry leads.
    /// </summary>
    [DataField]
    public Vector2? InteriorCoords;
}

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
    /// Optional list of valid entry points relative to the vehicle's local rotation.
    /// If empty, any interaction with the component is allowed.
    /// </summary>
    [DataField]
    public List<VehicleEntryPoint> EntryPoints = new();

    /// <summary>
    /// Do-after duration (in seconds) before entering is completed.
    /// </summary>
    [DataField]
    public float EnterDoAfter = 0f;

    /// <summary>
    /// Do-after duration (in seconds) before exiting is completed.
    /// </summary>
    [DataField]
    public float ExitDoAfter = 0f;

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
public sealed partial class VehicleExitComponent : Component
{
    /// <summary>
    /// Optional index to map this exit to an exterior entry point.
    /// </summary>
    [DataField]
    public int EntryIndex;
}

[Serializable, NetSerializable]
public sealed partial class VehicleEnterDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public int EntryIndex;

    public override DoAfterEvent Clone()
    {
        return new VehicleEnterDoAfterEvent
        {
            EntryIndex = EntryIndex,
        };
    }
}

[Serializable, NetSerializable]
public sealed partial class VehicleExitDoAfterEvent : SimpleDoAfterEvent;

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
