using Content.Shared._RMC14.Botany;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Cardiopeutic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Treats heart damage. Forces new chemical mutations to occur in plants.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        EnableMutationSlot(args.EntityManager, plant, "New Chems", 1f);
        EnableMutationSlot(args.EntityManager, plant, "New Chems2", 1f);
        EnableMutationSlot(args.EntityManager, plant, "New Chems3", 1f);
    }

    // TODO RMC14: heals 0.5 * potency human heart dmg per tick, applies a flat 3 pain per 2s tick on critical overdose
    // (builds up) & recalculates pain when it leaves the body

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
