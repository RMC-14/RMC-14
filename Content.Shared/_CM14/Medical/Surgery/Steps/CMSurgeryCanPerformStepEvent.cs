namespace Content.Shared._CM14.Medical.Surgery.Steps;

[ByRefEvent]
public record struct CMSurgeryCanPerformStepEvent(List<EntityUid> Tools, string? Popup = null, bool Cancelled = false);
