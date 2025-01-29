namespace Content.Shared._RMC14.Xenonids;

[ByRefEvent]
public readonly record struct XenoHealAttemptEvent(bool Cancelled = false);
