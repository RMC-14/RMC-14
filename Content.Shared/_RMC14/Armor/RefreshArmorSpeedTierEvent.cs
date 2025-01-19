using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Armor;

[ByRefEvent]
public record struct RefreshArmorSpeedTierEvent(
    SlotFlags TargetSlots,
    string? SpeedTier = ""
) : IInventoryRelayEvent;
