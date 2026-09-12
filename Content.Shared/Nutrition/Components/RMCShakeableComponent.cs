using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
///     This is en extension of the upstream SliceableFoodComponent
/// </summary>
public sealed partial class ShakeableComponent
{
    /// <summary>
    ///     The skill that modifies the doafter delay when slicing food.
    /// </summary>
    [DataField]
    public EntProtoId<SkillDefinitionComponent> ShakeSkill = "RMCSkillDomestics";
}
