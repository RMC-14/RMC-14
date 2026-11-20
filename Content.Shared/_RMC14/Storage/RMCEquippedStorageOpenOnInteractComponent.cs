using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

/// <summary>
/// Works if it's on a storage equipped or on an entity by itself
/// The latter will make any storage in access slots or self if that's set
/// no matter if the storage itself has this comp
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCEquippedStorageOpenOnInteractComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags AccessSlots = SlotFlags.BACK; // First come first serve

    [DataField, AutoNetworkedField]
    public bool CountSelf = false;

    /// <summary>
    /// Only works if count self is true and this isn't relayed, ofc
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireEquipped = true;
}
