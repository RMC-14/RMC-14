namespace Content.Shared._CM14.Medical.Surgery;

/// <summary>
///     Raised on the entity that is receiving surgery.
/// </summary>
[ByRefEvent]
public record struct CMSurgeryStepEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools);
