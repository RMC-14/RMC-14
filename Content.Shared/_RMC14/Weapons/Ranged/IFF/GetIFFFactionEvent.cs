using Content.Shared.Inventory;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[ByRefEvent]
public record struct GetIFFFactionEvent(
    HashSet<ProtoId<NpcFactionPrototype>>? Factions,
    SlotFlags TargetSlots
) : IInventoryRelayEvent;
