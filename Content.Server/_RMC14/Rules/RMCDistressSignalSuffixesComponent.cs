namespace Content.Server._RMC14.Rules;

[RegisterComponent]
[Access(typeof(CMDistressSignalRuleSystem))]
public sealed partial class RMCDistressSignalSuffixesComponent : Component
{
    [DataField]
    public HashSet<string> Suffixes = new();
}
