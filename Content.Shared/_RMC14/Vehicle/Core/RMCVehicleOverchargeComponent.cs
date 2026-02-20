using System;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleOverchargeComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1.6f;

    [DataField]
    public float Duration = 3f;

    [DataField]
    public float Cooldown = 16f;

    [DataField]
    public SoundSpecifier? OverchargeSound;

    [AutoNetworkedField]
    public TimeSpan ActiveUntil;

    [AutoNetworkedField]
    public TimeSpan CooldownUntil;
}
