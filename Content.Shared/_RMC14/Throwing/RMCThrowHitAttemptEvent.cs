namespace Content.Shared._RMC14.Throwing;

/// <summary>
/// Raised on the thrown entity before hit events run.
/// Cancel to skip both hit events.
/// </summary>
[ByRefEvent]
public record struct RMCThrowHitAttemptEvent(bool Cancelled = false);
