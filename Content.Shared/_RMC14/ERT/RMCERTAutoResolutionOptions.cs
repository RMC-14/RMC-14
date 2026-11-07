namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Optional automatic response policy shared by ERT request sources and handheld beacon prototypes.
/// Omitting the policy leaves the request for an administrator.
/// </summary>
[DataDefinition]
public sealed partial class RMCERTAutoResolutionOptions
{
    /// <summary>
    /// Time available for an administrator to respond before automatic resolution.
    /// Zero resolves immediately; a negative delay disables automatic resolution.
    /// </summary>
    [DataField]
    public TimeSpan Delay;

    /// <summary>
    /// Approval probability, clamped to 0..1. One guarantees approval without a success roll.
    /// </summary>
    [DataField]
    public float ApprovalChance = 1f;

    /// <summary>
    /// Optional display/log actor label supplied by API callers, rather than prototype data.
    /// </summary>
    public string? ActorName;
}
