using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sound;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMSoundSystem))]
public sealed partial class SoundOnDeathSoundComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Parent;
}
