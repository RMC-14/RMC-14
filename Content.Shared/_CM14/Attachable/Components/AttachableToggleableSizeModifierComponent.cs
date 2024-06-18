using Robust.Shared.GameStates;
using Robust.Shared.Utility;


namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableSizeModifierSystem))]
public sealed partial class AttachableToggleableSizeModifierComponent : Component
{
    [DataField("activeSizeModifier"), AutoNetworkedField]
    public int ActiveSizeModifier = 0;
	
    [DataField("inactiveSizeModifier"), AutoNetworkedField]
	public int InactiveSizeModifier = 0;
    
    public int ResetIncrement = 0;
}
