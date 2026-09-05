using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.GameTicking;

namespace Content.Server._RMC14.Rules;

[RegisterComponent]
[Access(typeof(CMDistressSignalRuleSystem))]
public sealed partial class CMDistressSignalSpawningComponent : Component
{
    public MetaJobAssignment SurvivorAssignment = new MetaJobAssignment("Survivor");
}
