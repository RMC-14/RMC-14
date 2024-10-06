using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Shuttles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlaySoundOnFTLStartComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}
