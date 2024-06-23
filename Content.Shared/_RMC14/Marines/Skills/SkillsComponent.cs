using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Skills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class SkillsComponent : Component
{
    [IncludeDataField, AutoNetworkedField]
    public Skills Skills;
}
