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
    /// What sound to play when a xeno is prying a podlock
    /// </summary>
    [DataField]
    public SoundSpecifier XenoPodlockUseSound = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");
}
