using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableSizeModifierSystem))]
public sealed partial class AttachableToggleableSizeModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ActiveSizeModifier = 0;

    [DataField, AutoNetworkedField]
	public int InactiveSizeModifier = 0;

    public int ResetIncrement = 0;
}
