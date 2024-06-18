using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;


namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableToggleableSystem))]
public sealed partial class AttachableToggleableComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = false;
    
    [DataField, AutoNetworkedField]
    public float DoAfter = 0.0f;
    
    [DataField, AutoNetworkedField]
    public bool NeedHand = true;
    
    [DataField, AutoNetworkedField]
    public bool BreakOnMove = true;
    
    [DataField, AutoNetworkedField]
    public bool SupercedeHolder = false;
    
    [DataField, AutoNetworkedField]
    public bool AttachedOnly = false;
    
    //[DataField, AutoNetworkedField]
    //public SoundSpecifier? DetachSound = new SoundPathSpecifier("/Audio/_CM14/Attachable/attachment_remove.ogg");
}
