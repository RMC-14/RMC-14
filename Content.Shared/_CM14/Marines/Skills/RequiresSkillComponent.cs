using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class RequiresSkillComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Skills Skills;
}
