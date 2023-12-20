using Content.Shared.Inventory;

namespace Content.Shared._CM14.Xenos.Projectile.Spit.Slowing;

[ByRefEvent]
public record struct HitBySlowingSpitEvent(SlotFlags TargetSlots, bool Cancelled = false) : IInventoryRelayEvent;
