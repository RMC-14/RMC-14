namespace Content.Shared.Warps;

/// <summary>
/// Raised after the display location of a warp point changes.
/// </summary>
public readonly record struct WarpPointLocationChangedEvent(string? OldValue, string? NewValue);
