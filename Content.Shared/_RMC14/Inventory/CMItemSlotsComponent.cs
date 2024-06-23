using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Inventory;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMItemSlotsComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastEjectAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? Cooldown;

    [DataField, AutoNetworkedField]
    public string? CooldownPopup;

    [DataField, AutoNetworkedField]
    public int? Count;

    [DataField, AutoNetworkedField]
    public ItemSlot? Slot;

    [DataField, AutoNetworkedField]
    public EntProtoId? StartingItem;
}
