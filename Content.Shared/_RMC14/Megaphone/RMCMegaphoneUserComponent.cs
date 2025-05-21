using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMegaphoneSystem))]
public sealed partial class RMCMegaphoneUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? OnSpeakSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");
}
