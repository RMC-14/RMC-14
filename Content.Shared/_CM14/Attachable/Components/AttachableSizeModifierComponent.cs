using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableSizeModifierSystem))]
public sealed partial class AttachableSizeModifierComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public int SizeModifier = 0;

    public int ResetIncrement = 0;
}
