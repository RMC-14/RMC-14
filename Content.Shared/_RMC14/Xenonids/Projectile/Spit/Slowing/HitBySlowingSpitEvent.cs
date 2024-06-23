using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Slowing;

[ByRefEvent]
public record struct HitBySlowingSpitEvent(SlotFlags TargetSlots, bool Cancelled = false) : IInventoryRelayEvent;
