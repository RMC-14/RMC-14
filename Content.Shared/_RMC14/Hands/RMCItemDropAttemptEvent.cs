namespace Content.Shared._RMC14.Hands;

[ByRefEvent]
public record struct RMCItemDropAttemptEvent(bool Cancelled = false);
