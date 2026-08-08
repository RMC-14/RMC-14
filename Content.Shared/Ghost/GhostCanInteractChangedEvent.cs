namespace Content.Shared.Ghost;

/// <summary>
/// Raised after a ghost's interaction visibility has changed.
/// </summary>
public readonly record struct GhostCanInteractChangedEvent(bool OldValue, bool NewValue);
