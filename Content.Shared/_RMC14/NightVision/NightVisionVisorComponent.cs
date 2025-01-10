using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionVisorComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOn = new SoundPathSpecifier("/Audio/_RMC14/Handling/toggle_nv1.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundOff = new SoundPathSpecifier("/Audio/_RMC14/Handling/toggle_nv2.ogg");
}
