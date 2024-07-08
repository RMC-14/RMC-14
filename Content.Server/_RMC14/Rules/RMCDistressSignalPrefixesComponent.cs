namespace Content.Server._RMC14.Rules;

[RegisterComponent]
[Access(typeof(CMDistressSignalRuleSystem))]
public sealed partial class RMCDistressSignalPrefixesComponent : Component
{
    [DataField]
    public HashSet<string> Prefixes = new();
}
