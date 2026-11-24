// using Content.Server.Body.Components;
using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

/// <summary>
///     Requires that the metabolizing organ is or is not tagged with a certain MetabolizerType
/// </summary>
public sealed partial class OrganType : EventEntityEffectCondition<OrganType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type = default!;

    /// <summary>
    ///     Does this condition pass when the organ has the type, or when it doesn't have the type?
    /// </summary>
    [DataField]
    public bool ShouldHave = true;

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-organ-type",
            ("name", prototype.Index<MetabolizerTypePrototype>(Type).LocalizedName),
            ("shouldhave", ShouldHave));
    }
}
