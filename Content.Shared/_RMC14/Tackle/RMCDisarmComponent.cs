using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TackleSystem))]
public sealed partial class RMCDisarmComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillCqc";

    [DataField, AutoNetworkedField]
    public int AccidentalDischargeSkillAmount = 2;

    [DataField, AutoNetworkedField]
    public float AccidentalDischargeChance = 0.2f;

    [DataField, AutoNetworkedField]
    public TimeSpan BaseStunTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public List<LocId> RandomShoveTexts = new List<LocId>
    {
        "rmc-disarm-text-1",
        "rmc-disarm-text-2"
    };
}
