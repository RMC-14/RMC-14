using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Medical.Pain;

namespace Content.Server._RMC14.EntityEffects.Effects;

public sealed partial class PainLevel : EntityEffectCondition
{
    [DataField]
    public int Level;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out PainComponent? pain))
        {
            if (pain.CurrentPainLevel == Level)
                return true;
        }

        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "aboba";
    }
}
