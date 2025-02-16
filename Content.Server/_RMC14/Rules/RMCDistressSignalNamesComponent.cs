using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Rules;

[RegisterComponent]
[Access(typeof(CMDistressSignalRuleSystem))]
[EntityCategory("DistressSignalNames")]
public sealed partial class RMCDistressSignalNamesComponent : Component
{
    [DataField]
    public HashSet<string> Names = new();
}
