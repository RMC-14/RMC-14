using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction.Upgrades;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCUpgradeSystem))]
public sealed partial class RMCConstructionUpgradeTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId[]? Upgrades;

    [DataField, AutoNetworkedField]
    public EntProtoId? Downgrade;

    [DataField]
    public TimeSpan DowngradeTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillConstruction";

    [DataField, AutoNetworkedField]
    public int SkillAmountRequired = 1;
}

[Serializable, NetSerializable]
public sealed partial class DowngradeDoAfterEvent : SimpleDoAfterEvent
{

}
