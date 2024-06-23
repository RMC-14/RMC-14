using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class RemoveRecentDefibrillation : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.Scale < 0.95f)
            return;

        args.EntityManager.RemoveComponentDeferred<CMRecentlyDefibrillatedComponent>(args.SolutionEntity);
    }
}
