using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class ReagentExaminationRequiresSkillComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId? UnskilledExamine;

    [DataField(required: true), AutoNetworkedField]
    public LocId SkilledExamineContains;

    [DataField(required: true), AutoNetworkedField]
    public LocId SkilledExamineNone;

    [DataField(required: true), AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> Skills = new();

    [DataField, AutoNetworkedField]
    public string? ContainerId;

    [DataField, AutoNetworkedField]
    public LocId? NoContainerExamine;
}
