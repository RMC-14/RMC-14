using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Atrichogenic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Damages the hair follicles in the skin to disrupt the hair growth cycle, causing extreme alopecia, also referred to as baldness.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.TargetEntity, out var humanoid))
            return;

        var changed = false;
        if (humanoid.MarkingSet.RemoveCategory(MarkingCategories.Hair))
            changed = true;

        if (humanoid.MarkingSet.RemoveCategory(MarkingCategories.FacialHair))
            changed = true;

        if (!changed)
            return;

        args.EntityManager.Dirty(args.TargetEntity, humanoid);

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("chem-atrichogenic-hair-loss"), args.TargetEntity, args.TargetEntity, PopupType.SmallCaution);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[CellularType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[CellularType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
