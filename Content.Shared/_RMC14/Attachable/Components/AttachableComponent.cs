using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderSystem))]
public sealed partial class AttachableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float AttachDoAfter = 1.5f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AttachSound = new SoundPathSpecifier("/Audio/_RMC14/Attachable/attachment_add.ogg", AudioParams.Default.WithVolume(-6.5f));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DetachSound = new SoundPathSpecifier("/Audio/_RMC14/Attachable/attachment_remove.ogg",  AudioParams.Default.WithVolume(-5.5f));
}
