using Content.Shared._RMC14.Temperature;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Cryometabolizing : RMCChemicalEffect, IReagentBooster
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Boosts the potency of reagents by [color=green]{Potency * 0.5f}[/color] while body temperature is below 210 °K or -63.15 °C";
    }

    public float CalculateBoost(EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var temperatureSystem = entityManager.System<SharedRMCTemperatureSystem>();

        if (temperatureSystem.TryGetCurrentTemperature(target, out var bodyTemperature) && bodyTemperature <= 210)
        {
            return Potency * 0.5f;
        }
        return 0f;
    }
}
