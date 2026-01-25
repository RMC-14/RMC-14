using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Visor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VisorSystem))]
public sealed partial class ToggleVisorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IgnoreRedundancy = false;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOn = new SoundPathSpecifier("/Audio/_RMC14/Handling/hud_on.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOff = new SoundPathSpecifier("/Audio/_RMC14/Handling/hud_off.ogg");
}
