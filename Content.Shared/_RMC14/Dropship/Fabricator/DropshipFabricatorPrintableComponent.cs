using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship.Fabricator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(DropshipFabricatorSystem))]
public sealed partial class DropshipFabricatorPrintableComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Cost = 50;

    [DataField, AutoNetworkedField]
    public float RecycleMultiplier = 0.8f;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> RecycleSkill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public CategoryType Category;

    public enum CategoryType
    {
        Equipment,
        Ammo,
    }
}
