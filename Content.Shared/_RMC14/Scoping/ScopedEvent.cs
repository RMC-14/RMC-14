using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Scoping;

[ByRefEvent]
public readonly record struct ScopedEvent(
    EntityUid User,
    Entity<ScopeComponent> Scope,
    SlotFlags TargetSlots = SlotFlags.HEAD
) : IInventoryRelayEvent;
