using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sound;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CMSoundSystem))]
public sealed partial class RandomSoundComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? Sound;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan Min;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan Max;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? PlayAt;
}
