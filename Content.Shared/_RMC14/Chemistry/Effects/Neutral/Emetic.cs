using Content.Shared._RMC14.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Emetic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Induces emesis, the forceful emptying of the stomach.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Source is not { } source || args.Reagent is not { } reagent)
            return;

        var quantity = source.GetTotalPrototypeQuantity(reagent.ID);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob((float) ActualPotency * (float) quantity * 0.005f))
        {
            var vomit = args.EntityManager.System<RMCVomitSystem>();
            vomit.StartVomit(args.TargetEntity);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
