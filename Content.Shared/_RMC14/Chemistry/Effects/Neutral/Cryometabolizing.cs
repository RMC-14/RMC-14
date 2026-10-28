using Content.Shared._RMC14.Temperature;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Cryometabolizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "The chemical is passively metabolized with no other effects in temperatures above 210 kelvin. Below however, the chemical will metabolize with increased effect.";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        if (temperature.GetTemperature(args.TargetEntity) <= 210f)
            boost += Potency * 0.5f;
    }

    protected override bool ShouldCancel(EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        return temperature.GetTemperature(args.TargetEntity) > 210f;
    }
}
