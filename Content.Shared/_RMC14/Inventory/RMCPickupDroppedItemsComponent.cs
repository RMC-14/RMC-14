using Robust.Shared.Collections;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Inventory;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class RMCPickupDroppedItemsComponent : Component
{
    [DataField, AutoNetworkedField]
    public ValueList<EntityUid> DroppedItems = new();
}
