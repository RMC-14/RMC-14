using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Skills;

/// <summary>
/// A preset of a group of skills. Useful for standardization of skills across roles that have the same skillset.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class SkillPresetComponent : Component
{
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> Skills = new();
}
