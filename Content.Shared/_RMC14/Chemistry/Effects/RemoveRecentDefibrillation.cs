using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class RemoveRecentDefibrillation : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagent && reagent.Scale < 0.95f)
            return;

        args.EntityManager.RemoveComponentDeferred<CMRecentlyDefibrillatedComponent>(args.TargetEntity);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}
