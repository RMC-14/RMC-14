using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction.Upgrades;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCUpgradeSystem))]
public sealed partial class RMCConstructionUpgradeTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId[]? Upgrades;

    [DataField, AutoNetworkedField]
    public EntProtoId? Downgrade;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillConstruction";

    [DataField, AutoNetworkedField]
    public int SkillAmountRequired = 1;
}
