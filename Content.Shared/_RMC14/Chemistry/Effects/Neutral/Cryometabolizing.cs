using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared._RMC14.Temperature;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Cryometabolizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Boosts the potency of reagents by [color=green]{ReagentBoost}[/color] while body temperature is below 210 °K or -63.15 °C";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var temperatureSystem = entityManager.System<SharedRMCTemperatureSystem>();

        if (!temperatureSystem.TryGetCurrentTemperature(target, out var bodyTemperature) || bodyTemperature > 210)
            return;
        ReagentBoost = Potency * 0.5f;
    }
}
