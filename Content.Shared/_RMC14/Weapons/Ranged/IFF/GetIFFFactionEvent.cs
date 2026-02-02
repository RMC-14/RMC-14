using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[ByRefEvent]
public record struct GetIFFFactionEvent(
    SlotFlags TargetSlots,
    HashSet<EntProtoId<IFFFactionComponent>> Factions
) : IInventoryRelayEvent;
