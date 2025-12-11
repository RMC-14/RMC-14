using System;
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
}
