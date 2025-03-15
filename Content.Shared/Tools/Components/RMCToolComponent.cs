using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tools.Components;

/// <summary>
///     This is en extension of the upstream ToolComponent
/// </summary>
public sealed partial class ToolComponent
{
    /// <summary>
    ///     The skill that modifies the doafter delay when using this tool.
    /// </summary>
    [DataField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillEngineer";
}
