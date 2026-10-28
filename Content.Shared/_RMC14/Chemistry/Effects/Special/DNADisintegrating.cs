using Content.Shared._RMC14.Chemistry.XenoTransformation;
using Content.Shared._RMC14.Synth;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class DNADisintegrating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";
    private static readonly FixedPoint2 TransformationThreshold = 150;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Immediately disintegrates the DNA of all organic cells it comes into contact with.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[CellularType] = potency * 10f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var entities = args.EntityManager;
        var target = args.TargetEntity;
        if (!entities.HasComponent<HumanoidAppearanceComponent>(target) ||
            entities.HasComponent<SynthComponent>(target) ||
            !entities.TryGetComponent(target, out DamageableComponent? damageableComp) ||
            !damageableComp.Damage.DamageDict.TryGetValue(CellularType, out var cellular) ||
            cellular < TransformationThreshold)
        {
            return;
        }

        // TODO RMC14: also fracture random limb every tick while clone damage is >= 150 - cm13: fracture(100) on a
        // random limb per 2s tick, a guaranteed break unless fracture resistance active
        entities.System<RMCXenoTransformationSystem>().TryContract(target);
    }

    // TODO RMC14: DNA_Disintegrating/trigger() fires once, from the reagent analyzer, the
    // 1st time this property is scanned/discovered - it calls WY "Chemical Investigation Squad"
}
