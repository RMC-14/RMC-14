namespace Content.Shared._RMC14.Storage;

/// <summary>
/// Used to set visibility on open and closed layers when empty, open, or closed.
/// </summary>
[RegisterComponent]
public sealed partial class CMStorageVisualizerComponent : Component
{
    /// <summary>
    /// Sprite layer name of the closed state.
    /// </summary>
    [DataField]
    public string? StorageClosed;

    /// <summary>
    /// Sprite layer name of the open state.
    /// </summary>
    [DataField]
    public string? StorageOpen;

    /// <summary>
    /// Sprite layer name of the empty state.
    /// </summary>
    [DataField]
    public string? StorageEmpty;

    /// <summary>
    /// If true, the storage will still use its open/closed layers while empty instead of
    /// short-circuiting to an empty-only presentation.
    /// </summary>
    [DataField]
    public bool ShowOpenClosedWhenEmpty;
}
