using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Drowsyness;
using Content.Shared._RMC14.Mute;
using Content.Shared.Damage;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Focusing : RMCChemicalEffect
{
    public override string Abbreviation => "FCS";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var focusing = Potency >= 3
            ? ". Also powerful enough to instantly cure mute and blindness."
            : ".";

        return $"Removes [color=green]{PotencyPerSecond}[/color] units of alcoholic substances and [color=green]{PotencyPerSecond * 2}[/color] seconds of drunkenness{focusing}\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 3}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcBloodstream = System<SharedRMCBloodstreamSystem>(args);
        var stutter = System<SharedStutteringSystem>(args);
        var drunk = System<SharedDrunkSystem>(args);
        var drowsy = System<DrowsynessSystem>(args);
        var status = System<SharedStatusEffectsSystem>(args);

        rmcBloodstream.RemoveBloodstreamAlcohols(args.TargetEntity, potency);
        stutter.DoRemoveStutterTime(args.TargetEntity, PotencyPerSecond * 2);
        drunk.TryRemoveDrunkenessTime(args.TargetEntity, PotencyPerSecond * 2);
        drowsy.TryChange(args.TargetEntity, PotencyPerSecond * -2);
        status.TryAddTime(args.TargetEntity, "Jitter", TimeSpan.FromSeconds(PotencyPerSecond * -2)); // TODO RMC14 amplitude frequency
        // TODO RMC14 M.ReduceEyeBlur(PotencyPerSecond * 2) remove blur without healing eyes

        if (Potency >= 3)
        {
            if (TryComp(args, out BlindableComponent? blindable))
            {
                // TODO RMC14 M.SetEyeBlind(0) remove blind without healing eyes
                var blindableSys = System<BlindableSystem>(args);
                blindableSys.AdjustEyeDamage((args.TargetEntity, blindable), -blindable.EyeDamage); // negative to heal
            }

            args.EntityManager.RemoveComponent<RMCMutedComponent>(args.TargetEntity);
            args.EntityManager.RemoveComponent<MutedComponent>(args.TargetEntity);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, PoisonType, potency * 3);
    }
}
