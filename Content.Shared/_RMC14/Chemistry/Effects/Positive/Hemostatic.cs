using Content.Shared._RMC14.Chemistry.ChemEffectFlags;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Hemostatic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Vastly improves the blood's natural ability to coagulate and stop bleeding. Overdosing causes extreme blood clotting, resulting in severe tissue damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.HasComponent<BloodstreamComponent>(args.TargetEntity))
            return;

        args.EntityManager.System<RMCChemEffectFlagsSystem>().TrySetFlag(args.TargetEntity, RMCChemEffectFlagsSystem.NoBleedingFlag);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency * 9f;
        damage.DamageDict[HeatType] = potency * 9f;
        damage.DamageDict[PoisonType] = potency * 9f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
