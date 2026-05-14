using System.Linq;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Trichogenic : RMCChemicalEffect
{
    public override string Abbreviation => "TRI";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Causes random and excessive hair growth.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] brute damage with a {PotencyPerSecond * 2.5:F2}% chance.";//\n" +
               //$"Critical overdoses cause [color=red]{PotencyPerSecond}[/color] brain damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!ProbHundred(2.5 * potency))
            return;

        if (!TryComp(args, out HumanoidAppearanceComponent? humanoid))
            return;

        var markingManager = IoCManager.Resolve<MarkingManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        var hairMarkings = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.Hair, humanoid.Species, humanoid.Sex).Values.ToList();
        if (hairMarkings.Count > 0)
        {
            humanoid.MarkingSet.RemoveCategory(MarkingCategories.Hair);
            humanoid.MarkingSet.AddBack(MarkingCategories.Hair, random.Pick(hairMarkings).AsMarking());
        }

        var facialHairMarkings = markingManager.MarkingsByCategoryAndSpeciesAndSex(MarkingCategories.FacialHair, humanoid.Species, humanoid.Sex).Values.ToList();
        if (facialHairMarkings.Count > 0)
        {
            humanoid.MarkingSet.RemoveCategory(MarkingCategories.FacialHair);
            humanoid.MarkingSet.AddBack(MarkingCategories.FacialHair, random.Pick(facialHairMarkings).AsMarking());
        }

        args.EntityManager.Dirty(args.TargetEntity, humanoid);

        var popup = System<SharedPopupSystem>(args);
        var net = IoCManager.Resolve<INetManager>();
        if (net.IsServer)
            popup.PopupClient("Your head feels different...", args.TargetEntity, args.TargetEntity);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (ProbHundred(2.5 * potency))
        {
            // TODO RMC14 one limb at random
            TryChangeDamage(args, BluntType, potency);

            var popup = System<SharedPopupSystem>(args);
            var net = IoCManager.Resolve<INetManager>();
            if (net.IsServer)
                popup.PopupClient("You feel itchy all over!", args.TargetEntity, args.TargetEntity, PopupType.MediumCaution);
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 Brain Damage

        var popup = System<SharedPopupSystem>(args);
        var net = IoCManager.Resolve<INetManager>();
        if (net.IsServer)
            popup.PopupClient("You feel like something is penetrating your skull!", args.TargetEntity, args.TargetEntity);
    }
}
