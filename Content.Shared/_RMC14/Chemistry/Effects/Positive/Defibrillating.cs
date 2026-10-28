using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Defibrillating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";
    private static readonly ProtoId<DamageTypePrototype> CellularType = "Cellular";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    private static readonly ProtoId<EmotePrototype> Twitch = "RMCTwitch";

    private static readonly TimeSpan ElectrogeneticInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan NotifyCooldown = TimeSpan.FromSeconds(10);

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes an electrochemical reaction in the cardiac muscles, forcing the heart to continue pumping. May cause irregular heart rhythms.";
    }

    // TODO RMC14: pain.apply_pain(PROPERTY_DEFIBRILLATING_PAIN_OD = 2) on overdose (a flat 2 pain per 2s tick that
    // builds up), & pain.recalculate_pain()
    // when the chemical leaves the body
    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    // TODO RMC14: mob effect - heart organ damage on critical overdose - 0.5 * potency * delta_time (potency per 2s tick)

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (entities.TryGetComponent(target, out MobStateComponent? mobStateComp) &&
            entities.System<MobStateSystem>().IsDead(target, mobStateComp))
        {
            return;
        }

        entities.RemoveComponent<RMCDefibrillatingReviveComponent>(target);
        entities.RemoveComponent<RMCDefibrillatingComponent>(target);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
            entities.System<SharedRMCEmoteSystem>().TryEmoteWithChat(target, Twitch, hideLog: true, forceEmote: true);
    }

    protected override bool TickDead(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        if (!entities.HasComponent<MobStateComponent>(target) ||
            !entities.HasComponent<HumanoidAppearanceComponent>(target))
        {
            return false;
        }

        var rotting = entities.System<SharedRottingSystem>();
        if (rotting.IsRotten(target) || entities.HasComponent<UnrevivableComponent>(target))
            return false;

        var mobThreshold = entities.System<MobThresholdSystem>();
        if (!mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var deadThreshold) ||
            !entities.TryGetComponent(target, out DamageableComponent? damageableComp))
        {
            return false;
        }

        var timing = IoCManager.Resolve<IGameTiming>();

        if (entities.HasComponent<RMCDefibrillatingReviveComponent>(target))
            return false;

        var defibrillating = entities.EnsureComponent<RMCDefibrillatingComponent>(target);
        if (timing.CurTime >= defibrillating.NextElectrogeneticAt)
        {
            defibrillating.NextElectrogeneticAt = timing.CurTime + ElectrogeneticInterval;
            if (entities.System<SharedRMCDefibrillatorSystem>().TryConsumeElectrogenetic(target, out var electrogeneticHeal))
                damageable.TryChangeDamage(target, electrogeneticHeal, true, interruptsDoAfters: false);
        }

        if (damageableComp.TotalDamage < deadThreshold)
        {
            var revive = entities.EnsureComponent<RMCDefibrillatingReviveComponent>(target);
            revive.RevivesAt = timing.CurTime + TimeSpan.FromSeconds(5);

            if (timing.CurTime < defibrillating.NextNotifyAt)
                return false;

            defibrillating.NextNotifyAt = timing.CurTime + NotifyCooldown;
            var ev = new RMCDefibrillatingReviveStartedEvent();
            entities.EventBus.RaiseLocalEvent(target, ref ev);
            return true;
        }

        if (ActualPotency < 1)
            return true;

        if (IoCManager.Resolve<IRobustRandom>().Prob(0.5f))
            entities.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-defibrillating-faint-spark"), target, target);

        var groupHeal = potency * 0.25f;
        if (ActualPotency > 2)
            groupHeal += potency * 0.5f;

        var rmcDamageable = entities.System<SharedRMCDamageableSystem>();
        var heal = new DamageSpecifier();
        heal = rmcDamageable.DistributeHealingCached((target, damageableComp), BruteGroup, groupHeal, heal);
        heal = rmcDamageable.DistributeHealingCached((target, damageableComp), BurnGroup, groupHeal, heal);
        heal = rmcDamageable.DistributeHealingCached((target, damageableComp), ToxinGroup, groupHeal, heal);
        heal.DamageDict[CellularType] = -groupHeal;

        if (damageableComp.Damage.DamageDict.TryGetValue(AsphyxiationType, out var oxyLoss) && oxyLoss > FixedPoint2.Zero)
            heal.DamageDict[AsphyxiationType] = -oxyLoss;

        damageable.TryChangeDamage(target, heal, true, interruptsDoAfters: false);
        return true;
    }
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RMCDefibrillatingReviveComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RevivesAt;
}

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RMCDefibrillatingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextElectrogeneticAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextNotifyAt;
}

[ByRefEvent]
public readonly record struct RMCDefibrillatingReviveStartedEvent;
