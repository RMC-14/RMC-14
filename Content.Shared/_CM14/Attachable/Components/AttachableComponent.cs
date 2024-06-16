using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableHolderSystem))]
public sealed partial class AttachableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float AttachDoAfter = 1.5f;
    
    [DataField, AutoNetworkedField]
    public SoundSpecifier? AttachSound = new SoundPathSpecifier("/Audio/_CM14/Attachable/attachment_add.ogg");
    
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DetachSound = new SoundPathSpecifier("/Audio/_CM14/Attachable/attachment_remove.ogg");
}
