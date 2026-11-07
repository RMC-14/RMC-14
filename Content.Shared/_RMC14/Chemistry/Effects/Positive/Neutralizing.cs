using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neutralizing : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    private const float PlasmaDrainMultiplier = 5f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Neutralizes certain reactive chemicals and plasmas on contact. Unsafe to administer intravenously.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(args);
            return;
        }

        var damage = new DamageSpecifier();
        damage.DamageDict[HeatType] = potency;
        damage.DamageDict[PoisonType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    private void HandleReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (entities.HasComponent<MobStateComponent>(target))
        {
            if (entities.TryGetComponent<FlammableComponent>(target, out var flammable))
            {
                var flammableSystem = entities.System<SharedRMCFlammableSystem>();
                flammableSystem.Extinguish((target, flammable));
            }

            if (entities.TryGetComponent<XenoPlasmaComponent>(target, out var plasma))
            {
                var plasmaSystem = entities.System<XenoPlasmaSystem>();
                var drain = FixedPoint2.New(PlasmaDrainMultiplier * (float) args.Quantity * ActualPotency);
                plasmaSystem.RemovePlasma((target, plasma), drain);
                entities.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-neutralizing-plasma-drained"), target, target, PopupType.SmallCaution);
            }

            return;
        }

        var acid = entities.System<SharedXenoAcidSystem>();
        if (acid.IsMelted(target))
            acid.RemoveAcid(target);

        if (entities.HasComponent<TileFireComponent>(target))
            entities.QueueDeleteEntity(target);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[HeatType] = potency * 2f;
        damage.DamageDict[PoisonType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO RMC14: liver damage on critical overdose - potency liver damage per 2s tick
}
