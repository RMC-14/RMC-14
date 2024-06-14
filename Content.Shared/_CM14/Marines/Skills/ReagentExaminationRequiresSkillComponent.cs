using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Skills;

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
    public Skills Skills;
}
