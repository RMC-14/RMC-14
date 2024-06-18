using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Weapons.Ranged.IFF;

[ByRefEvent]
public record struct IsInIFFFactionEvent(EntProtoId Faction, bool InFaction = false, SlotFlags TargetSlots = SlotFlags.IDCARD)
    : IInventoryRelayEvent
{
    public void TryHandle(EntProtoId? id)
    {
        if (InFaction)
            return;

        if (id == Faction)
            InFaction = true;
    }
}
