using Content.Shared.Inventory;

namespace Content.Shared._RMC14.Wieldable;

[ByRefEvent]
public record struct RefreshWieldSlowdownCompensationEvent(
    SlotFlags TargetSlots,
    float Walk = 0f,
    float Sprint = 0f
) : IInventoryRelayEvent;
