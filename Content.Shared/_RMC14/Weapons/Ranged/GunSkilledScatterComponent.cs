using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class GunSkilledScatterComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillFirearms";

    [DataField, AutoNetworkedField]
    public Angle UnskilledAngleIncrease = Angle.FromDegrees(75);

    [DataField, AutoNetworkedField]
    public int SkilledMinimum = 1;

    [DataField, AutoNetworkedField]
    public float SkillMultiplier = 6; // user.skills.get_skill_level(SKILL_FIREARMS)*SCATTER_AMOUNT_TIER_8, SCATTER_AMOUNT_TIER_8 is multiplied by 2 as per SS13 conversions
}
