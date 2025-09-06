namespace Content.Shared._RMC14.Xenonids;

[ByRefEvent]
public record struct XenoHealAttemptEvent(bool Cancelled = false);
