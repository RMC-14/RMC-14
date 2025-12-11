using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCBattleExecuteComponent : Component
{
    /// <summary>
    ///     Skill requirement to perform a BE with this weapon
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillExecution";

    /// <summary>
    ///     The BE's Damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new() { DamageDict = { ["Blunt"] = 200 } };

    /// <summary>
    ///     How long it takes to perform a BE Do-After
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BattleExecuteTimeSeconds = TimeSpan.FromSeconds(1);
}
