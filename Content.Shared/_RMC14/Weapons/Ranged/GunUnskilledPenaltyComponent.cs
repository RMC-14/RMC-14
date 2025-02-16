using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunUnskilledPenaltyComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Firearms = 1;

    [DataField, AutoNetworkedField]
    public Angle AngleIncrease = Angle.FromDegrees(75);

    [DataField, AutoNetworkedField]
    public FixedPoint2 AccuracyAddMult = -0.15;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillFirearms";
}
