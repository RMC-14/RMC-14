using Content.Shared.Inventory;

namespace Content.Shared._CM14.Xenos.Spit.Slowing;

[ByRefEvent]
public record struct HitBySlowingSpitEvent(SlotFlags TargetSlots, bool Cancelled = false) : IInventoryRelayEvent;
