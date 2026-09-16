using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Corrosive : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond}[/color] burn damage.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond * 2}[/color] burn damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] burn damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.Method == ReactionMethod.Touch)
        {
            HandleTouch(damageable, args);
            return;
        }

        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BurnGroup, potency);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    private static bool IsUnacidable(IEntityManager entities, EntityUid? uid)
    {
        return entities.TryGetComponent(uid, out CorrodibleComponent? corrodible) && !corrodible.IsCorrodible;
    }

    private void HandleTouch(DamageableSystem damageable, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.HasComponent<MobStateComponent>(target))
        {
            HandleTouchItem(args);
            return;
        }

        var meltChance = (float) ActualPotency * 0.03f;
        var isHuman = entities.HasComponent<HumanoidAppearanceComponent>(target) && !entities.HasComponent<SynthComponent>(target);
        if (isHuman)
        {
            var inventory = entities.System<InventorySystem>();
            var popup = entities.System<SharedPopupSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            if (inventory.TryGetSlotEntity(target, "head", out var head))
            {
                if (random.Prob(meltChance) && !IsUnacidable(entities, head))
                {
                    inventory.TryUnequip(target, "head", force: true);
                    entities.QueueDeleteEntity(head);
                    popup.PopupEntity("Your headgear melts away but protects you from the acid!", target, target, PopupType.MediumCaution);
                }
                else
                {
                    popup.PopupEntity("Your headgear protects you from the acid.", target, target, PopupType.Small);
                }

                return;
            }

            if (inventory.TryGetSlotEntity(target, "mask", out var mask))
            {
                if (random.Prob(meltChance) && !IsUnacidable(entities, mask))
                {
                    inventory.TryUnequip(target, "mask", force: true);
                    entities.QueueDeleteEntity(mask);
                    popup.PopupEntity("Your mask melts away but protects you from the acid!", target, target, PopupType.MediumCaution);
                }
                else
                {
                    popup.PopupEntity("Your mask protects you from the acid.", target, target, PopupType.Small);
                }

                return;
            }

            if (inventory.TryGetSlotEntity(target, "eyes", out var eyes))
            {
                if (random.Prob(meltChance) && !IsUnacidable(entities, eyes))
                {
                    inventory.TryUnequip(target, "eyes", force: true);
                    entities.QueueDeleteEntity(eyes);
                    popup.PopupEntity("Your eyewear melts away!", target, target, PopupType.MediumCaution);
                }

                return;
            }
        }

        var volume = (float) args.Quantity;

        // TODO RMC14: volume>=10 special case that deals flat damage (4 brute + 2 burn)
        // to humans head limb specifically, with a chance to scream, once no headgear/mask/eyewear
        // is left to melt

        var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeDamageCached(target, BruteGroup, MathF.Min(6f, volume));
        damageable.TryChangeDamage(target, damage, true, interruptsDoAfters: false);

        if (entities.HasComponent<XenoComponent>(target) && ActualPotency > 2f)
        {
            var toxicBuildup = entities.System<RMCToxicBuildupSystem>();
            var magnitude = ActualPotency * volume * 0.25f;
            toxicBuildup.AddBuildup(target, magnitude, 1f / 3f, 75f);
        }
    }

    private void HandleTouchItem(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.HasComponent<ItemComponent>(target))
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob((float) ActualPotency * 0.1f))
            entities.QueueDeleteEntity(target);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BurnGroup, potency * 2f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var damage = rmcDamageable.DistributeFreshDamage(BurnGroup, potency * 5f);
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Comp.Tray is not { } trayUid || GetTray(args.EntityManager, plant) is not { } tray)
            return;

        var plantTray = args.EntityManager.System<SharedRMCPlantTraySystem>();
        var amount = Potency * (float) args.Quantity;
        if (tray.WeedLevel > 0)
            plantTray.SetWeedLevel((trayUid, tray), MathF.Max(0f, tray.WeedLevel - amount));

        if (tray.PestLevel > 0)
            plantTray.SetPestLevel((trayUid, tray), MathF.Max(0f, tray.PestLevel - amount));
    }
}
