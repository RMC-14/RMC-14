using Content.Shared._RMC14.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Focusing : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var focusing = ActualPotency >= 3
            ? ". Also powerful enough to instantly cure mute and blindness."
            : ".";

        return $"Removes [color=green]{PotencyPerSecond}[/color] units of alcoholic substances and [color=green]{PotencyPerSecond * 2}[/color] seconds of drunkenness{focusing}\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] toxin damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 3}[/color] toxin damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        var drunkSystem = args.EntityManager.System<SharedDrunkSystem>();
        var stutterSystem = args.EntityManager.System<SharedStutteringSystem>();
        var statusEffects = args.EntityManager.System<SharedStatusEffectsSystem>();

        bloodstream.RemoveBloodstreamAlcohols(args.TargetEntity, potency);
        drunkSystem.TryRemoveDrunkenessTime(args.TargetEntity, PotencyPerSecond * 2);
        stutterSystem.DoRemoveStutterTime(args.TargetEntity, PotencyPerSecond * 2);
        statusEffects.TryAddTime(args.TargetEntity, "Jitter", TimeSpan.FromSeconds(PotencyPerSecond * -2));
        // ReduceEyeBlur(PotencyPerSecond * 2) but BlurryVisionComponent is sealed so only healing the eyes will remove blur.

        if (!(ActualPotency >= 3))
            return;
        args.EntityManager.EntitySysManager.GetEntitySystem<BlindableSystem>().AdjustEyeDamage(args.TargetEntity, -9);
        args.EntityManager.RemoveComponent<MutedComponent>(args.TargetEntity);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 3;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
