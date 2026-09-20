using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCEquippedStorageOpenOnInteractComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags AccessSlots = SlotFlags.BACK;
}
