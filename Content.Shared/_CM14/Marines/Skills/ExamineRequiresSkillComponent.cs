using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Skills;

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
    public Skills Skills;
}
