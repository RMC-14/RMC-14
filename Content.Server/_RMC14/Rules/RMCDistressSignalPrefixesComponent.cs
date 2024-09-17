using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Rules;

[RegisterComponent]
[Access(typeof(CMDistressSignalRuleSystem))]
[EntityCategory("DistressSignalNames")]
public sealed partial class RMCDistressSignalPrefixesComponent : Component
{
    [DataField]
    public HashSet<string> Prefixes = new();
}
