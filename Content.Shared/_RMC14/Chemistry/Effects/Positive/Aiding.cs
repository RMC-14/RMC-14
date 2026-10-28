using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Disabilities;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Aiding : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";

    private static readonly EntProtoId ConfusedStatus = "RMCStatusEffectConfused";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Fixes genetic defects, disfigurements and disabilities. In plants, removes compounds modifying yield and mutation.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        RMCDisabilities.ClearAll(args.EntityManager, args.TargetEntity);
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var scaled = Potency * (float) args.Quantity;
        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        plantTray.AdjustMutationMod((plant.Owner, plant.Comp), -4f * scaled);
        AddYieldMod(args.EntityManager, plant, -4f * scaled);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryUpdateStatusEffectDuration(args.TargetEntity, ConfusedStatus, TimeSpan.FromSeconds(ActualPotency * 40f));

        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(ToxinGroup, potency);
        damage.DamageDict[CellularType] = potency * 3f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(ActualPotency * 40f));
    }
}
