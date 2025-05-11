using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TackleSystem))]
public sealed partial class TackleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? KnockdownSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_knockdown.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
}
