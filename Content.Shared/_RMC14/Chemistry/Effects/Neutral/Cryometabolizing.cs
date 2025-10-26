using Content.Shared._RMC14.Temperature;
using Content.Shared.EntityEffects;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

/// <summary>
/// Chemical effect that boosts reagent potency when cold, but prevents metabolism when warm.
/// Implements CMSS13's cryometabolizing behavior with REAGENT_CANCEL.
/// </summary>
public sealed partial class Cryometabolizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var celsius = TemperatureHelpers.KelvinToCelsius(CryoLiquidThreshold);
        return $"Prevents reagent metabolism above {CryoLiquidThreshold} °K ({celsius:0.##} °C).\n" +
               $"Below this temperature, boosts the potency of reagents by [color=green]{Potency * 0.5f}[/color].";
    }

    protected override void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
        var tempSystem = args.EntityManager.System<SharedRMCTemperatureSystem>();
        var currentTemp = tempSystem.GetTemperature(args.TargetEntity);

        // Above threshold: prevent metabolism (REAGENT_CANCEL in CMSS13)
        if (currentTemp > CryoLiquidThreshold)
        {
            // Mark this reagent to not be metabolized this tick
            var preventComp = args.EntityManager.EnsureComponent<PreventMetabolismComponent>(args.TargetEntity);
            if (args.Reagent != null)
                preventComp.PreventedReagents.Add(args.Reagent.ID);
            return;
        }

        // Below threshold: boost potency (REAGENT_BOOST in CMSS13)
        boost = Potency * 0.5f;
    }
}
