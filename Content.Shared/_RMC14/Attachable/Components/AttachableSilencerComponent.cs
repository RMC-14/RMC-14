using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableSilencerSystem))]
public sealed partial class AttachableSilencerComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("CMSilencedShoot");

    [DataField, AutoNetworkedField]
    public bool HideMuzzleFlash = true;
}
