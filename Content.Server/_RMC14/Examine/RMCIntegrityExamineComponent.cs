using Robust.Shared.Localization;

namespace Content.Server._RMC14.Examine;

[RegisterComponent]
public sealed partial class RMCIntegrityExamineComponent : Component
{
    public LocId PercentMessage = "rmc-wall-integrity-percent";
}
