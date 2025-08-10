using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCBattleExecuteComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillExecution";

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new() { DamageDict = { ["Blunt"] = 200 } };

    [DataField, AutoNetworkedField]
    public int BattleExecuteTimeSeconds = 2;
}
