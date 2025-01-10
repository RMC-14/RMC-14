using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Inventory;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class RMCPickupDroppedItemsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> DroppedItems = new();
}
