using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

public sealed partial class HasTag : EntityEffectCondition
{
    [DataField]
    public ProtoId<TagPrototype> Tag = default!;

    [DataField]
    public bool Invert = false;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent<TagComponent>(args.TargetEntity, out var tag))
            return args.EntityManager.System<TagSystem>().HasTag(tag, Tag) ^ Invert;

        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        // this should somehow be made (much) nicer.
        return Loc.GetString("reagent-effect-condition-guidebook-has-tag", ("tag", Tag), ("invert", Invert));
    }
}
