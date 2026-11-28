using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
///    Penalities to apply to the gun if the user is unskilled, or bonuses if they are skilled.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunSkilledScatterComponent : Component
{
    /// <summary>
    ///    Skill to check for.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillFirearms";

    /// <summary>
    ///    Penalty to apply if the user doesn't meet the SkilledMinimum.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle UnskilledAngleIncrease = Angle.FromDegrees(8);

    [DataField, AutoNetworkedField]
    public int SkilledMinimum = 1;

    /// <summary>
    ///    This number is multiplied by the user's skill level for scatter reduction in degrees.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SkillMultiplier = 6; // user.skills.get_skill_level(SKILL_FIREARMS)*SCATTER_AMOUNT_TIER_8, SCATTER_AMOUNT_TIER_8 is multiplied by 2 as per SS13 conversions
}
