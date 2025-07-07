using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPodDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Id;

    /// <summary>
    /// What sound to play when a xeno is prying a podlock
    /// </summary>
    [DataField]
    public SoundSpecifier XenoPodlockUseSound = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");

    /// <summary>
    /// The doafter time multiplier for when a xeno is prying a podlock
    /// </summary>
    [DataField]
    public float XenoPodlockPryMultiplier = 4.0f;
}

[ByRefEvent]
public record struct PodlockPryEvent(EntityUid User)
{
    public readonly EntityUid User = User;

    public bool Cancelled;
}
