using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SkillsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Surgery;
}
