using Content.Shared._CM14.Medical.Defibrillator;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Chemistry.Effects;

public sealed partial class RemoveRecentDefibrillation : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }

    public override void Effect(ReagentEffectArgs args)
    {
        args.EntityManager.RemoveComponentDeferred<CMRecentlyDefibrillatedComponent>(args.SolutionEntity);
    }
}
