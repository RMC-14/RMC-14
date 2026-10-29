using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Barricade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCFoldingBarricadeSystem))]
public sealed partial class RMCFoldingBarricadeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId FoldedPrototype = "CMFoldingBarricade";

    [DataField, AutoNetworkedField]
    public float MaxDamage = 350;

    [DataField, AutoNetworkedField]
    public TimeSpan CollapseDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan CrowbarCollapseDelay = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> CrowbarSkill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public int CrowbarSkillRequired = 1;
}
