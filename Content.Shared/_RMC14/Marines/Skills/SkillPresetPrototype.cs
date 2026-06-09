using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Skills;

/// <summary>
/// A preset of a group of skills. Useful for standardization of skills across roles that have the same skillset.
/// </summary>
[Prototype]
public sealed partial class SkillPresetPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> Skills = new();
}
