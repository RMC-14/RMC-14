using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Sound;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMSoundSystem))]
public sealed partial class SoundOnDeathComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? Sound;
}
