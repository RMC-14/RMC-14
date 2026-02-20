using System;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCVehicleSmashableComponent : Component
{
    [DataField]
    public bool DeleteOnHit = true;

    [DataField]
    public float SlowdownMultiplier = 0.5f;

    [DataField]
    public float SlowdownDuration = 0.5f;

    [DataField]
    public SoundSpecifier? SmashSound;
}
