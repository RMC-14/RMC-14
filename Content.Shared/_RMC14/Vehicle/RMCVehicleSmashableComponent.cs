using System;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

/// <summary>
/// Marks an entity as being destroyed when a vehicle collides with it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCVehicleSmashableComponent : Component
{
    /// <summary>
    /// If true, the entity will be deleted when hit.
    /// </summary>
    [DataField]
    public bool DeleteOnHit = true;

    /// <summary>
    /// Multiplier applied to the vehicle's current speed after smashing this entity.
    /// Use 1.0 for no slowdown, 0 to stop completely.
    /// </summary>
    [DataField]
    public float SlowdownMultiplier = 0.5f;

    /// <summary>
    /// Duration in seconds the slowdown should apply for. Set to 0 to disable slowing.
    /// </summary>
    [DataField]
    public float SlowdownDuration = 0.5f;

    /// <summary>
    /// Optional sound played when this entity is smashed by a vehicle.
    /// </summary>
    [DataField]
    public SoundSpecifier? SmashSound;
}
