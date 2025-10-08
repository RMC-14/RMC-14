namespace Content.Shared._RMC14.Pulling;

[ByRefEvent]
public record struct RMCGetPullTargetEvent(EntityUid User, EntityUid Target);
