using System;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleSoundComponent : Component
{
    [DataField]
    public SoundSpecifier? RunningSound;

    [DataField]
    public float RunningSoundCooldown = 1f;

    [AutoNetworkedField]
    public TimeSpan NextRunningSound;
}
