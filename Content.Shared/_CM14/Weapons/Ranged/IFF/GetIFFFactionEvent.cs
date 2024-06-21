using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Weapons.Ranged.IFF;

[ByRefEvent]
public record struct GetIFFFactionEvent(
    EntProtoId<IFFFactionComponent>? Faction,
    SlotFlags TargetSlots
) : IInventoryRelayEvent;
