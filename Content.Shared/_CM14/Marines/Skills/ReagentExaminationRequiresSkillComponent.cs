using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Skills;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class ReagentExaminationRequiresSkillComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Skills Skills;
}
