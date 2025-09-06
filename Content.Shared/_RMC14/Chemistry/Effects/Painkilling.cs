using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Medical.Pain;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class Painkilling : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> PoisonGroup = "Poison";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    private static readonly EntProtoId<StatusEffectComponent> SeeingRainbows = "StatusEffectSeeingRainbow";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond * 2;
        return Loc.GetString("reagent-effect-guidebook-painkilling",
            ("pain", ActualPotency * 40),
            ("toxin", PotencyPerSecond),
            ("hall-seconds", ActualPotency * 2));
    }

    // TODO: opiate receptor deficiency
    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var scale = (args as EntityEffectReagentArgs)?.Scale ?? 1;
        var painSystem = args.EntityManager.EntitySysManager.GetEntitySystem<PainSystem>();
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out PainComponent? pain))
            painSystem.AddPainModificator(args.TargetEntity, TimeSpan.FromSeconds(scale.Float()), ActualPotency * 40, PainModificatorType.PainReduction, pain);
    }

    // TODO: opiate receptor deficiency
    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TrySetStatusEffectDuration(args.TargetEntity, SeeingRainbows, TimeSpan.FromSeconds(ActualPotency * 2));

        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonGroup] = potency * 2; // potency * delta_time at 13, delta_time cancels the effect that ticks at 13 are 2 times slower than at 14
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO: opiate receptor deficiency, damage to liver and brain
    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AirlossGroup] = 1.5; // literally constant 3 from 13 divided by two because tics of reagents at 14 2 times faster
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
