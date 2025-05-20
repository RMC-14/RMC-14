using Content.Shared.Interaction;

namespace Content.Shared._RMC14.Inventory;

[ByRefEvent]
public record struct ShouldHandleVirtualItemInteractEvent(BeforeRangedInteractEvent Event, EntityUid Blocking, bool Handle = false);
