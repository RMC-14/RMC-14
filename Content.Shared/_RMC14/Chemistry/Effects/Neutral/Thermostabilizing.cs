using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Thermostabilizing : RMCChemicalEffect
{
    private static readonly ProtoId<StatusEffectPrototype> Unconscious = "Unconscious";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Stabilizes the temperature of the body to [color=green]{TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature)}[/color] kelvins, by [color=green]{40f * PotencyPerSecond * 1.5f}[/color] K at a time.\n" +
               $"Overdoses cause [color=red]10[/color] seconds of unconsciousness.\n" +
               $"Critical overdoses cause [color=red]5[/color] seconds of unconsciousness with a [color=red]5%[/color] chance";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedRMCTemperatureSystem>();
        var current = sys.GetTemperature(args.TargetEntity);
        var normalBodyTemp = TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature);
        if (Math.Abs(current - normalBodyTemp) < 0.01)
            return;

        var change = 40f * potency.Float() * 1.5f;

        var temp = current > normalBodyTemp
            ? Math.Max(normalBodyTemp, current - change)
            : Math.Min(normalBodyTemp, current + change);

        sys.ForceChangeTemperature(args.TargetEntity, temp);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<RMCUnconsciousComponent>(
            args.TargetEntity,
            Unconscious,
            TimeSpan.FromSeconds(40),
            true
        );
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 Drowsiness. if drowsiness > 10 5% change to paralyze(knockout) for 10 seconds.
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f))
            return;

        var status = args.EntityManager.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<RMCUnconsciousComponent>(
            args.TargetEntity,
            Unconscious,
            TimeSpan.FromSeconds(10),
            true
        );
    }
}
