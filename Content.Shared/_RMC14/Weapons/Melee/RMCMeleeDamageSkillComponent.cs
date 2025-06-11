using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Melee;

/// <summary>
/// Adds a flat amount of damage based on the skill of the user.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCMeleeWeaponSystem))]
public sealed partial class RMCMeleeDamageSkillComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillCqc";

    [DataField, AutoNetworkedField]
    public ProtoId<DamageTypePrototype> BonusDamageType = "Blunt";
}
