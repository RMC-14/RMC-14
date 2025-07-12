using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Conditions;

public sealed partial class RecentlyDefibrillated : EntityEffectCondition
{
    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<CMRecentlyDefibrillatedComponent>(args.TargetEntity);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "defibrillated";
    }
}
