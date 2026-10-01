namespace Content.Shared._RMC14.Movement;

[ByRefEvent]
public record struct RMCAttemptMobPushedEvent(EntityUid Pusher, bool Cancelled = false);
