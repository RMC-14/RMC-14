namespace Content.Shared._RMC14.Throwing;

/// <summary>
///     Raised on the item entity that is thrown.
/// </summary>
/// <param name="User">The user that threw this entity.</param>
/// <param name="Cancelled">Whether or not the throw should be cancelled.</param>
[ByRefEvent]
public record struct ThrowItemAttemptEvent(EntityUid User, bool Cancelled = false);
