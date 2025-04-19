using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Armor.Magnetic;

[ByRefEvent]
public record struct RMCMagnetizeItemEvent(
    EntityUid User,
    SlotFlags MagnetizeToSlots,
    SlotFlags TargetSlots,
    EntityUid? Magnetizer = null
) : IInventoryRelayEvent;
