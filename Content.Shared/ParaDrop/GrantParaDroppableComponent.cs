using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.ParaDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrantParaDroppableComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.BACK;
}
