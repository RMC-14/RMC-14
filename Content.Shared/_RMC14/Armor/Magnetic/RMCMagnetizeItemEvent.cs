using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Armor.Magnetic;

[ByRefEvent]
public record struct RMCMagnetizeItemEvent(SlotFlags TargetSlots, EntityUid? Magnetizer = null) : IInventoryRelayEvent;
