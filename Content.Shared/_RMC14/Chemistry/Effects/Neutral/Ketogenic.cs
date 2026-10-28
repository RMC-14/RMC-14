using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Ketogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";
    private static readonly EntProtoId DrowsinessStatus = "StatusEffectDrowsiness";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes [color=red]{PotencyPerSecond * 5}[/color] nutrients, causing hunger over time.\n" +
               $"Increases alcohol metabolism rate by [color=green]{PotencyPerSecond}[/color] units.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 10}[/color] nutrition loss, [color=red]{PotencyPerSecond}[/color] toxin damage, and a [color=red]{ActualPotency * 2.5}%[/color] chance of vomiting.\n" +
               $"Critical overdoses will knock you out for [color=red]40[/color] seconds";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();

        hungerSystem.ModifyHunger(target, PotencyPerSecond * -5);

        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        var alcoholRemoved = bloodstream.RemoveBloodstreamAlcohols(args.TargetEntity, potency);

        if (!alcoholRemoved)
            return;

        var status = entityManager.System<SharedStatusEffectsSystem>();
        var timing = IoCManager.Resolve<IGameTiming>();

        ApplyCapped(status, timing, target, ConfusedStatus, (float) potency * 2f, 20f * ActualPotency);
        ApplyCapped(status, timing, target, DrowsinessStatus, (float) potency, 15f * ActualPotency);
    }

    private static void ApplyCapped(
        SharedStatusEffectsSystem status,
        IGameTiming timing,
        EntityUid target,
        EntProtoId proto,
        float increment,
        float cap)
    {
        if (cap <= 0f)
            return;

        var current = 0f;
        if (status.TryGetTime(target, proto, out var time) && time.EndEffectTime is { } end)
        {
            var remaining = (float) (end - timing.CurTime).TotalSeconds;
            if (remaining > 0f)
                current = remaining;
        }

        var next = Math.Min(current + increment, cap);
        if (next <= 0f)
            return;

        status.TrySetStatusEffectDuration(target, proto, TimeSpan.FromSeconds(next));
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var hungerSystem = entityManager.System<HungerSystem>();
        hungerSystem.ModifyHunger(target, PotencyPerSecond * -10);

        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damageable.TryChangeDamage(target, damage, true, interruptsDoAfters: false);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.025f * ActualPotency))
        {
            entityManager.System<RMCVomitSystem>().StartVomit(target);
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40));
    }
}
