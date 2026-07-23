using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Thermostabilizing : RMCChemicalEffect
{
    public override string Abbreviation => "TSL";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Stabilizes body temperature to [color=green]{Atmospherics.NormalBodyTemperature}ºC[/color], by [color=green]{40f * PotencyPerSecond * 1.5f}ºC[/color] at a time.\n" +
               $"Overdoses cause [color=red]40[/color] seconds of unconsciousness.\n" +
               $"Critical overdoses cause a [color=red]5%[/color] chance to inflict [color=red]10[/color] seconds of unconsciousness";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var sys = System<SharedRMCTemperatureSystem>(args);
        var current = sys.GetTemperature(args.TargetEntity);
        var normalBodyTemp = TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature);
        if (Math.Abs(current - normalBodyTemp) < 0.01)
            return;

        var change = (40f * potency * 1.5f * args.Scale).Float();
        var temp = current > normalBodyTemp
            ? Math.Max(normalBodyTemp, current - change)
            : Math.Min(normalBodyTemp, current + change);

        sys.ForceChangeTemperature(args.TargetEntity, temp);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = System<RMCSizeStunSystem>(args);
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40), true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 Drowsiness. if drowsiness > 10 5% change to paralyze(knockout) for 10 seconds.
        var knockOut = System<RMCSizeStunSystem>(args);
        if (ProbHundred(5))
            knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(10), true);
    }
}
