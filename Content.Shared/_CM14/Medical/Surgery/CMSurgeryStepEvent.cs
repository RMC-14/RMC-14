namespace Content.Shared._CM14.Medical.Surgery;

/// <summary>
///     Raised on the step entity.
/// </summary>
[ByRefEvent]
public record struct CMSurgeryStepEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools);
