using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Atrichogenic : RMCChemicalEffect
{
    public override string Abbreviation => "ATR";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Causes extreme alopecia, also referred to as baldness.\n" +
               $"Overdoses cause [color=red]{0.5 * PotencyPerSecond}[/color] genetic damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] genetic damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!TryComp(args, out HumanoidAppearanceComponent? humanoid))
            return;

        var changed = humanoid.MarkingSet.RemoveCategory(MarkingCategories.Hair);
        changed |= humanoid.MarkingSet.RemoveCategory(MarkingCategories.FacialHair);

        if (changed)
        {
            args.EntityManager.Dirty(args.TargetEntity, humanoid);

            var popup = System<SharedPopupSystem>(args);
            var net = IoCManager.Resolve<INetManager>();
            if (net.IsServer)
                popup.PopupEntity("Your hair falls out!", args.TargetEntity, args.TargetEntity);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, GeneticType, 0.5 * potency);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, GeneticType, potency);
    }
}
