using Robust.Shared.Localization;

namespace Content.Server._RMC14.Examine;

/// <summary>
///     Shows an integrity percentage on examine.
/// </summary>
[RegisterComponent]
public sealed partial class RMCIntegrityExamineComponent : Component
{
    [DataField("percentMessage")]
    public LocId PercentMessage = "rmc-wall-integrity-percent";
}
