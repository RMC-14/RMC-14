using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class ExamineRequiresSkillComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId? UnskilledExamine;

    [DataField(required: true), AutoNetworkedField]
    public LocId SkilledExamine;

    [DataField, AutoNetworkedField]
    public int ExaminePriority = 1000;

    [DataField(required: true), AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> Skills = new();
}
