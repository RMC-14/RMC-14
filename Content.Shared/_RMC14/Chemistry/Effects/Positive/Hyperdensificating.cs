using Content.Shared._RMC14.Chemistry.ChemEffectFlags;
using Content.Shared._RMC14.Chemistry.SpeedModifier;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Hyperdensificating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the muscles and bones to become super dense, providing superior resistance to bone fractures.";
    }

    // TODO RMC14: fracture resistance
    // check for ResistFractureStatus
    // sets fracture chance to 0 & blocks limbs from being cut or torn off while active
    public static readonly EntProtoId ResistFractureStatus = "RMCStatusEffectResistFracture";

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        if (!entities.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity) ||
            entities.HasComponent<SynthComponent>(args.TargetEntity))
        {
            return;
        }

        // cm13 CHEM_EFFECT_RESIST_FRACTURE is a chem_effect_flag, so it lingers until the next 16s flag reset
        // after the chemical is gone. The prob(5 * delta_time) message only rolls when the flag wasnt already
        // set (once per reset cycle)
        if (!entities.System<RMCChemEffectFlagsSystem>().TrySetFlag(args.TargetEntity, ResistFractureStatus))
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.1f))
            entities.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-hyperdensificating-tense"), args.TargetEntity, args.TargetEntity);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
            args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-hyperdensificating-hard-to-move"), args.TargetEntity, args.TargetEntity);

        var speedModifier = args.EntityManager.System<RMCReagentSpeedModifierSystem>();
        speedModifier.SetSpeedModifier(args.TargetEntity, $"{args.Reagent?.ID}:{nameof(Hyperdensificating)}", 2f * ActualPotency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BruteGroup, potency * 3f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
