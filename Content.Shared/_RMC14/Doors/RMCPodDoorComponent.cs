using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMDoorSystem))]
public sealed partial class RMCPodDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Id;

    /// <summary>
    /// The doafter time multiplier for when a xeno is prying a podlock
    /// </summary>
    [DataField]
    public float XenoPodlockPryMultiplier = 4.0f;
}
