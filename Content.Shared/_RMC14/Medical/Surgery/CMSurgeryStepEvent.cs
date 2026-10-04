namespace Content.Shared._RMC14.Medical.Surgery;

/// <summary>
///     Raised on the step entity.
/// </summary>
[ByRefEvent]
public record struct CMSurgeryStepEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools);

/// <summary>
///     Raised on the step entity when the step roll fails.
/// </summary>
[ByRefEvent]
public record struct CMSurgeryStepFailedEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools);
