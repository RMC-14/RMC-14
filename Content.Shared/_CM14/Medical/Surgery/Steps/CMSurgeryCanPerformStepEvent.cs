using Content.Shared.Inventory;

namespace Content.Shared._CM14.Medical.Surgery.Steps;

[ByRefEvent]
public record struct CMSurgeryCanPerformStepEvent(
    EntityUid User,
    EntityUid Body,
    List<EntityUid> Tools,
    SlotFlags TargetSlots,
    string? Popup = null,
    StepInvalidReason Invalid = StepInvalidReason.None,
    HashSet<EntityUid>? ValidTools = null
) : IInventoryRelayEvent;
