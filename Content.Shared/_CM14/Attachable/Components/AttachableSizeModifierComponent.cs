using Robust.Shared.GameStates;
using Robust.Shared.Utility;


namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableSizeModifierSystem))]
public sealed partial class AttachableSizeModifierComponent : Component
{
    [DataField("sizeModifier", required:true), AutoNetworkedField]
    public int SizeModifier = 0;
    
    public int ResetIncrement = 0;
}
