namespace Content.Shared._CM14.Medical.Surgery.Steps;

[ByRefEvent]
public record struct CMSurgeryCanPerformStepEvent(
    EntityUid User,
    List<EntityUid> Tools,
    string? Popup = null,
    StepInvalidReason Invalid = StepInvalidReason.None,
    HashSet<EntityUid>? ValidTools = null
);
