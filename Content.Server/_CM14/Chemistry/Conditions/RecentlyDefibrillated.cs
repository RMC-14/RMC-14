using Content.Shared._CM14.Medical.Defibrillator;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Chemistry.Conditions;

public sealed partial class RecentlyDefibrillated : ReagentEffectCondition
{
    public override bool Condition(ReagentEffectArgs args)
    {
        return args.EntityManager.HasComponent<CMRecentlyDefibrillatedComponent>(args.SolutionEntity);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return "defibrillated";
    }
}
