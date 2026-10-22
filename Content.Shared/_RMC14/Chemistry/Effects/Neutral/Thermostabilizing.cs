using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Thermostabilizing : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Stabilizes the temperature of the body to [color=green]{TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature)}[/color] kelvins, by [color=green]{60f * PotencyPerSecond}[/color] K at a time.\n" +
               $"Overdoses cause a [color=red]40[/color] second knockdown.\n" +
               $"Critical overdoses cause drowsiness.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedRMCTemperatureSystem>();
        var current = sys.GetTemperature(args.TargetEntity);
        var normalBodyTemp = TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature);
        if (Math.Abs(current - normalBodyTemp) < 0.01)
            return;

        var change = 60f * potency.Float();

        var temp = current > normalBodyTemp
            ? Math.Max(normalBodyTemp, current - change)
            : Math.Min(normalBodyTemp, current + change);

        sys.ForceChangeTemperature(args.TargetEntity, temp);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40));
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var remaining = TimeSpan.Zero;
        if (status.TryGetTime(args.TargetEntity, "StatusEffectDrowsiness", out var time) && time.EndEffectTime is { } end)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            remaining = end - timing.CurTime;
        }

        var floor = TimeSpan.FromSeconds(30);
        if (remaining < floor)
            status.TrySetStatusEffectDuration(args.TargetEntity, "StatusEffectDrowsiness", floor);
    }
}
