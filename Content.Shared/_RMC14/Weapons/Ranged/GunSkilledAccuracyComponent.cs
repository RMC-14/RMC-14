using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class GunSkilledAccuracyComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillFirearms";

    [DataField, AutoNetworkedField]
    public FixedPoint2 AccuracyAddMult = 0.15;
}
