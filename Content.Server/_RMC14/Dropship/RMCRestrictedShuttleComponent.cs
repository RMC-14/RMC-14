namespace Content.Server._RMC14.Dropship;

/// <summary>
/// Marks a shuttle grid as using restricted routing metadata for launch and dock verification.
/// </summary>
[RegisterComponent]
public sealed partial class RMCRestrictedShuttleComponent : Component
{
    /// <summary>
    /// ERT request id that owns this restricted shuttle, when launched by ERT.
    /// </summary>
    [ViewVariables]
    public Guid RequestId;

    /// <summary>
    /// ERT call prototype id that configured this shuttle, when available.
    /// </summary>
    [ViewVariables]
    public string? Call;
}
