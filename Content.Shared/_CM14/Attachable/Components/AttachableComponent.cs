using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderSystem))]
public sealed partial class AttachableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float AttachDoAfter = 1.5f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AttachSound = new SoundPathSpecifier("/Audio/_CM14/Attachable/attachment_add.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DetachSound = new SoundPathSpecifier("/Audio/_CM14/Attachable/attachment_remove.ogg");
}
