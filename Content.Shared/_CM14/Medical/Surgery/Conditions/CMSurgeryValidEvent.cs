namespace Content.Shared._CM14.Medical.Surgery.Conditions;

/// <summary>
///     Raised on the entity that is receiving surgery.
/// </summary>
[ByRefEvent]
public record struct CMSurgeryValidEvent(EntityUid Body, EntityUid Part, bool Cancelled = false);
