using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Atmos;
using Content.Server._RMC14.Barricade;
using Content.Server._RMC14.NPC;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Spider;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed class RMCGiantLizardSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly DirectionalAttackBlockSystem _directionalBlock = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly RMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private readonly Dictionary<EntityUid, EntityUid> _lastFoodHolder = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGiantLizardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCGiantLizardComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RMCGiantLizardComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCGiantLizardComponent, InteractHandEvent>(OnInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCGiantLizardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCGiantLizardComponent, DisarmedEvent>(OnDisarmed);
        SubscribeLocalEvent<RMCGiantLizardComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCGiantLizardComponent, RMCGiantLizardPounceActionEvent>(OnPounceAction);
        SubscribeLocalEvent<RMCGiantLizardComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<RMCGiantLizardComponent, PhysicsSleepEvent>(OnPhysicsSleep);
        SubscribeLocalEvent<RMCGiantLizardComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<FoodComponent, GotEquippedHandEvent>(OnFoodPickedUp);
        SubscribeLocalEvent<FoodComponent, GotUnequippedHandEvent>(OnFoodDropped);
        SubscribeLocalEvent<FoodComponent, EntityTerminatingEvent>(OnFoodTerminating);
    }

    private void OnMapInit(Entity<RMCGiantLizardComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.PounceActionEntity, ent.Comp.PounceAction, ent.Owner);
        ent.Comp.NextUpdateAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.UpdateCooldown);
        ent.Comp.NextTongueFlickAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.TongueFlickCooldown);
        UpdateLizardVisuals(ent);
    }

    private void OnShutdown(Entity<RMCGiantLizardComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.PounceActionEntity);
    }

    private void OnDamageChanged(Entity<RMCGiantLizardComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        ent.Comp.LastHitAt = Timing.CurTime;
        WakeRest(ent);
        UpdateLizardVisuals(ent);

        if (args.Origin is not { } origin || origin == ent.Owner)
            return;

        TryAggro(ent.Owner, origin, ent.Comp);
        PlayGrowl(ent);
        AlertPack(ent.Owner, origin, ent.Comp);
        TryStartFightOrFlight(ent, origin);
    }

    private void OnInteractHand(Entity<RMCGiantLizardComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled ||
            args.User == ent.Owner ||
            !MobState.IsAlive(ent.Owner) ||
            IsOnFire(ent.Owner) ||
            !Faction.IsEntityFriendly(ent.Owner, args.User))
        {
            return;
        }

        if (!ent.Comp.Resting)
        {
            if (Random.Prob(ent.Comp.FriendlyPetRestChance))
                TryRest(ent);

            return;
        }

        if (ent.Comp.NextFriendlyPetEmoteAt > Timing.CurTime)
            return;

        ent.Comp.NextFriendlyPetEmoteAt = Timing.CurTime + RandomTime(ent.Comp.FriendlyPetEmoteCooldownMin, ent.Comp.FriendlyPetEmoteCooldownMax);
        Popup.PopupEntity(
            Loc.GetString(PickFriendlyPetPopup(), ("lizard", ent.Owner), ("user", args.User)),
            ent.Owner);

        if (!Random.Prob(ent.Comp.FriendlyPetHissChance))
            return;

        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);
        ShowTongueFlick(ent);
    }

    private void OnInteractUsing(Entity<RMCGiantLizardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsAcceptedLizardFood(args.Used))
            return;

        WakeRest(ent);
        HealFraction(ent.Owner, ent.Comp.DirectFeedHealFraction);
        TryTameToFeeder(ent.Owner, args.User, ent.Comp);
        QueueDel(args.Used);
        args.Handled = true;

        UpdateLizardVisuals(ent);
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-feed"), ent.Owner, args.User);
    }

    private void OnDisarmed(Entity<RMCGiantLizardComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled ||
            args.Target != ent.Owner ||
            !MobState.IsAlive(ent.Owner) ||
            ent.Comp.Leaping ||
            !Random.Prob(ent.Comp.DisarmKnockdownChance))
        {
            return;
        }

        args.Handled = true;
        args.IsStunned = true;
        WakeRest(ent);
        Stun.TryKnockdown(ent.Owner, ent.Comp.DisarmKnockdown, true);
        _audio.PlayPvs(ent.Comp.DisarmKnockdownSound, ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-disarmed", ("lizard", ent.Owner), ("user", args.Source)), ent.Owner);
        UpdateLizardVisuals(ent);
    }

    private void OnMobStateChanged(Entity<RMCGiantLizardComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateLizardVisuals(ent);
    }

    private void OnPounceAction(Entity<RMCGiantLizardComponent> ent, ref RMCGiantLizardPounceActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryPounce(ent, args.Target);
    }

    private void OnStartCollide(Entity<RMCGiantLizardComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.Leaping)
            return;

        if (TryApplyPounceHit(ent, args.OtherEntity))
            return;

        TryApplyPounceObjectHit(ent, args.OtherEntity);
    }

    private void OnPhysicsSleep(Entity<RMCGiantLizardComponent> ent, ref PhysicsSleepEvent args)
    {
        if (ent.Comp.Leaping)
            StopPounce(ent);
    }

    private void OnMeleeHit(Entity<RMCGiantLizardComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.User != ent.Owner || args.HitEntities.Count == 0)
            return;

        args.HitSoundOverride = Random.Prob(0.5f) ? ent.Comp.SlashAttackSound : ent.Comp.BiteAttackSound;

        EntityUid? firstLivingTarget = null;
        foreach (var target in args.HitEntities)
        {
            if (target == ent.Owner || !ValidLivingMob(target))
                continue;

            firstLivingTarget ??= target;
            TryAggro(ent.Owner, target, ent.Comp);
        }

        if (firstLivingTarget is not { } livingTarget)
            return;

        if (HasComp<XenoComponent>(livingTarget))
            args.BonusDamage += ent.Comp.MeleeXenoBonusDamage;

        if (!Random.Prob(ent.Comp.SkirmishChance))
            return;

        StartSkirmish(ent, livingTarget);
    }

    private void OnFoodPickedUp(Entity<FoodComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!IsAcceptedLizardFood(ent.Owner))
            return;

        var holderCoords = Transform.GetMapCoordinates(args.User);
        foreach (var lizard in Lookup.GetEntitiesInRange<RMCGiantLizardComponent>(holderCoords, 8f))
        {
            if (lizard.Comp.FoodTarget != ent.Owner ||
                ActorQuery.HasComp(lizard.Owner) ||
                !MobState.IsAlive(lizard.Owner))
            {
                continue;
            }

            TryHandleFoodHolder((lizard.Owner, lizard.Comp), ent.Owner);
        }
    }

    private void OnFoodDropped(Entity<FoodComponent> ent, ref GotUnequippedHandEvent args)
    {
        _lastFoodHolder[ent.Owner] = args.User;
    }

    private void OnFoodTerminating(Entity<FoodComponent> ent, ref EntityTerminatingEvent args)
    {
        _lastFoodHolder.Remove(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCGiantLizardComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var lizard, out var xform))
        {
            if (!MobState.IsAlive(uid))
                continue;

            var ent = (uid, lizard, xform);
            UpdatePossession(ent);

            if (ActorQuery.HasComp(uid))
                continue;

            if (lizard.Leaping)
            {
                UpdatePounce(ent);
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (UpdateRavage((uid, lizard)))
            {
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (UpdateRetreat(ent))
            {
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (UpdateSkirmish(ent))
            {
                UpdateTongueFlick((uid, lizard));
                UpdateLizardVisuals((uid, lizard));
                continue;
            }

            if (lizard.NextUpdateAt > now)
                continue;

            lizard.NextUpdateAt = now + lizard.UpdateCooldown;

            UpdateTongueFlick((uid, lizard));
            UpdateLizardVisuals((uid, lizard));

            if (TryFirePanic((uid, lizard)))
                continue;

            DecayAggression((uid, lizard));

            var target = PickLizardTarget(ent);
            if (target == null)
            {
                if (WarnOrAggroCloseThreat(ent))
                    continue;

                if (TryAiFeed(ent))
                    continue;

                TryRest((uid, lizard));
                continue;
            }

            WakeRest((uid, lizard));

            if (TryStartDesperateRetreat(ent, target.Value))
                continue;

            TryAggro(uid, target.Value, lizard);
            AlertPack(uid, target.Value, lizard);
            TryBreakNearbyObstacle(ent);

            var lizardCoords = Transform.GetMoverCoordinates(uid);
            var targetCoords = Transform.GetMoverCoordinates(target.Value);
            if (!lizardCoords.TryDistance(EntityManager, targetCoords, out var distance))
                continue;

            if (distance < lizard.MinPounceRange || distance > lizard.MaxPounceRange)
                continue;

            TryPounce((uid, lizard), targetCoords);
        }
    }

    private void UpdatePossession(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (ActorQuery.HasComp(ent.Owner))
        {
            WakeRest((ent.Owner, ent.Comp1));

            if (!ent.Comp1.SleepingForPossession)
            {
                RMCNpc.SleepNPC(ent.Owner);
                ent.Comp1.SleepingForPossession = true;
            }

            return;
        }

        if (!ent.Comp1.SleepingForPossession)
            return;

        RMCNpc.WakeNPC(ent.Owner);
        ent.Comp1.SleepingForPossession = false;
    }

    private void UpdatePounce(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var now = Timing.CurTime;
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, 0.9f))
        {
            if (TryApplyPounceHit((ent.Owner, ent.Comp1), mob.Owner))
                return;
        }

        if (ent.Comp1.PounceEndAt <= now)
            StopPounce((ent.Owner, ent.Comp1));
    }

    private bool TryApplyPounceHit(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (target == ent.Owner || !ValidLivingMob(target))
            return false;

        if (TryComp<RMCLeapProtectionComponent>(target, out var protection) &&
            TryBlockPounce(ent, target, protection))
        {
            return true;
        }

        if (_size.TryGetSize(target, out var targetSize) && targetSize >= RMCSizes.Big)
        {
            StopPounce(ent);
            Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-pounce-too-large", ("target", target)), target, ent.Owner, PopupType.MediumCaution);
            return true;
        }

        if (Faction.IsEntityFriendly(ent.Owner, target))
        {
            StopPounce(ent);
            return true;
        }

        Stun.TryKnockdown(target, ent.Comp.PounceKnockdown, true);
        _dazed.TryDaze(target, ent.Comp.RavageDaze, true);
        _cameraShake.ShakeCamera(target, 2, ent.Comp.RavageCameraShakeStrength);
        Damageable.TryChangeDamage(target, ent.Comp.PounceDamage, origin: ent.Owner, tool: ent.Owner);
        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);

        TryAggro(ent.Owner, target, ent.Comp);
        ent.Comp.RavageTarget = target;
        ent.Comp.RavageHitsLeft = ent.Comp.RavageHitCount;
        ent.Comp.NextRavageAt = Timing.CurTime + ent.Comp.RavageHitDelay;

        StopPounce(ent);
        return true;
    }

    private bool TryApplyPounceObjectHit(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (target == ent.Owner || TerminatingOrDeleted(target) || MobQuery.HasComp(target))
            return false;

        if (TryComp<RMCLeapProtectionComponent>(target, out var protection) &&
            TryBlockPounce(ent, target, protection))
        {
            return true;
        }

        if (!DamageableQuery.HasComp(target) ||
            ItemQuery.HasComp(target) ||
            !XformQuery.TryGetComponent(target, out var xform) ||
            !xform.Anchored)
        {
            return false;
        }

        if (HasComp<DirectionalAttackBlockerComponent>(target) &&
            !_directionalBlock.IsAttackBlocked(ent.Owner, target))
        {
            return false;
        }

        StopPounce(ent);
        Damageable.TryChangeDamage(target, ent.Comp.PounceObstacleDamage, origin: ent.Owner, tool: ent.Owner);
        Stun.TryKnockdown(ent.Owner, ent.Comp.PounceObstacleKnockdown, true);
        _size.KnockBack(ent.Owner, Transform.GetMapCoordinates(target), ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockbackSpeed, true);

        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-pounce-obstacle", ("lizard", ent.Owner), ("target", target)), ent.Owner, PopupType.MediumCaution);
        return true;
    }

    private bool TryBlockPounce(Entity<RMCGiantLizardComponent> ent, EntityUid blocker, RMCLeapProtectionComponent protection)
    {
        if (!protection.FullProtection &&
            !_directionalBlock.IsFacingTarget(blocker, ent.Owner, ent.Comp.PounceOrigin))
        {
            return false;
        }

        StopPounce(ent);

        var stun = protection.InherentStunDuration ?? protection.StunDuration;
        if (stun > TimeSpan.Zero)
        {
            Stun.TryKnockdown(ent.Owner, stun, true);
            Stun.TryStun(ent.Owner, stun, true);
        }
        else
        {
            Stun.TryKnockdown(ent.Owner, ent.Comp.PounceBlockedKnockdown, true);
        }

        _size.KnockBack(ent.Owner, Transform.GetMapCoordinates(blocker), ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockbackSpeed, true);
        _audio.PlayPvs(protection.InherentStunDuration != null ? protection.InherentBlockSound : protection.BlockSound, ent.Owner);

        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-pounce-blocked", ("lizard", ent.Owner), ("target", blocker)), ent.Owner, PopupType.MediumCaution);
        return true;
    }

    private void StopPounce(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.Leaping = false;
        ent.Comp.PounceTarget = null;
        StopMovement(ent.Owner);

        if (PhysicsQuery.TryComp(ent.Owner, out var physics))
            Physics.SetBodyStatus(ent.Owner, physics, BodyStatus.OnGround);
    }

    private bool UpdateRavage(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.RavageTarget is not { } target)
            return false;

        if (!ValidLivingMob(target) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(target), out var distance) ||
            distance > 1.75f ||
            (!_standing.IsDown(target) && !MobState.IsIncapacitated(target)))
        {
            ClearRavage(ent.Comp);
            return false;
        }

        if (ent.Comp.NextRavageAt > Timing.CurTime)
            return true;

        var damage = ent.Comp.RavageDamage;
        if (HasComp<XenoComponent>(target))
            damage += ent.Comp.XenoBonusDamage;

        Damageable.TryChangeDamage(target, damage, origin: ent.Owner, tool: ent.Owner);
        Stun.TryKnockdown(target, ent.Comp.RavageKnockdown, true);
        _dazed.TryDaze(target, ent.Comp.RavageDaze, true);
        _cameraShake.ShakeCamera(target, 2, ent.Comp.RavageCameraShakeStrength);

        ent.Comp.RavageHitsLeft--;
        if (ent.Comp.RavageHitsLeft <= 0)
        {
            if (ent.Comp.NextPounceAt > Timing.CurTime)
            {
                ent.Comp.NextPounceAt -= ent.Comp.RavageCooldownRefund;
                if (ent.Comp.NextPounceAt < Timing.CurTime)
                    ent.Comp.NextPounceAt = Timing.CurTime;
            }

            ClearRavage(ent.Comp);
            return false;
        }

        ent.Comp.NextRavageAt = Timing.CurTime + ent.Comp.RavageHitDelay;
        return true;
    }

    private static void ClearRavage(RMCGiantLizardComponent comp)
    {
        comp.RavageTarget = null;
        comp.RavageHitsLeft = 0;
    }

    private void StartSkirmish(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (ActorQuery.HasComp(ent.Owner) ||
            ent.Comp.Leaping ||
            ent.Comp.RavageTarget != null ||
            !ValidLivingMob(target))
        {
            return;
        }

        WakeRest(ent);
        ent.Comp.Skirmishing = true;
        ent.Comp.SkirmishTarget = target;
        ent.Comp.SkirmishUntil = Timing.CurTime + ent.Comp.SkirmishDuration;
        TryMoveAwayFrom(ent.Owner, Transform.GetMoverCoordinates(target), ent.Comp.SkirmishSpeed);
    }

    private bool UpdateSkirmish(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (!ent.Comp1.Skirmishing)
            return false;

        if (ent.Comp1.SkirmishUntil <= Timing.CurTime ||
            ent.Comp1.SkirmishTarget is not { } target ||
            !ValidLivingMob(target) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(target), out _))
        {
            StopSkirmish((ent.Owner, ent.Comp1));
            return false;
        }

        TryMoveAwayFrom(ent.Owner, Transform.GetMoverCoordinates(target), ent.Comp1.SkirmishSpeed);
        return true;
    }

    private void StopSkirmish(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.Skirmishing = false;
        ent.Comp.SkirmishTarget = null;
        StopMovement(ent.Owner);
    }

    private EntityUid? PickLizardTarget(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var target in Faction.GetNearbyHostiles((ent.Owner, null, null), ent.Comp1.TargetSearchRange))
        {
            if (!ValidLivingMob(target) || !XformQuery.TryGetComponent(target, out var targetXform))
                continue;

            var targetMap = Transform.GetMapCoordinates((target, targetXform));
            if (targetMap.MapId != mapCoords.MapId)
                continue;

            var distance = (targetMap.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = target;
            bestDistance = distance;
        }

        return best;
    }

    private bool WarnOrAggroCloseThreat(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? warning = null;
        var warningDistance = float.MaxValue;

        foreach (var target in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, ent.Comp1.WarningRange))
        {
            if (target.Owner == ent.Owner ||
                !ValidLivingMob(target.Owner) ||
                Faction.IsEntityFriendly(ent.Owner, target.Owner))
            {
                continue;
            }

            var targetCoords = Transform.GetMapCoordinates(target.Owner);
            var distance = (targetCoords.Position - mapCoords.Position).Length();
            if (distance <= ent.Comp1.AggroRange)
            {
                TryAggro(ent.Owner, target.Owner, ent.Comp1);
                PlayGrowl((ent.Owner, ent.Comp1));
                AlertPack(ent.Owner, target.Owner, ent.Comp1);
                return true;
            }

            if (distance >= warningDistance)
                continue;

            warning = target.Owner;
            warningDistance = distance;
        }

        if (warning == null || ent.Comp1.NextWarningAt > Timing.CurTime)
            return false;

        ent.Comp1.NextWarningAt = Timing.CurTime + ent.Comp1.WarningCooldown;
        PlayGrowl((ent.Owner, ent.Comp1));
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-warning"), ent.Owner, warning.Value, PopupType.MediumCaution);
        return false;
    }

    private bool TryPounce(Entity<RMCGiantLizardComponent> ent, EntityCoordinates destination)
    {
        var now = Timing.CurTime;
        if (ent.Comp.Leaping)
        {
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-already");
            return false;
        }

        if (ent.Comp.NextPounceAt > now)
        {
            var seconds = (int) Math.Ceiling((ent.Comp.NextPounceAt - now).TotalSeconds);
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-cooldown", ("seconds", seconds));
            return false;
        }

        if (!PhysicsQuery.TryComp(ent.Owner, out var physics))
            return false;

        var origin = Transform.GetMoverCoordinates(ent.Owner);
        if (!origin.TryDistance(EntityManager, destination, out var distance) ||
            distance < ent.Comp.MinPounceRange ||
            distance > ent.Comp.MaxPounceRange + 0.25f)
        {
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-range");
            return false;
        }

        var direction = destination.Position - origin.Position;
        if (direction.LengthSquared() < 0.01f)
        {
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-range");
            return false;
        }

        WakeRest(ent);

        ent.Comp.NextPounceAt = now + ent.Comp.PounceCooldown;
        ent.Comp.Leaping = true;
        ent.Comp.PounceOrigin = origin;
        ent.Comp.PounceEndAt = now + TimeSpan.FromSeconds(distance / Math.Max(1, ent.Comp.PounceStrength));

        Physics.ResetDynamics(ent.Owner, physics);
        Physics.ApplyLinearImpulse(ent.Owner, direction.Normalized() * ent.Comp.PounceStrength * physics.Mass, body: physics);
        Physics.SetBodyStatus(ent.Owner, physics, BodyStatus.InAir);
        return true;
    }

    private void PopupPounceFailure(Entity<RMCGiantLizardComponent> ent, string locId, params (string, object)[] args)
    {
        if (!ActorQuery.HasComp(ent.Owner))
            return;

        Popup.PopupEntity(Loc.GetString(locId, args), ent.Owner, ent.Owner, PopupType.SmallCaution);
    }

    private void TryStartFightOrFlight(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (!TryGetHealthFraction(ent.Owner, ent.Comp, out var healthFraction) ||
            healthFraction > ent.Comp.FightOrFlightHealthFraction)
        {
            return;
        }

        var chance = Math.Clamp(1f - healthFraction, 0f, 1f);
        TryStartRetreat(ent, target, chance);
    }

    private bool TryStartDesperateRetreat(Entity<RMCGiantLizardComponent, TransformComponent> ent, EntityUid target)
    {
        if (!IsLowHealth(ent.Owner, ent.Comp1))
            return false;

        return TryStartRetreat((ent.Owner, ent.Comp1), target, 1f);
    }

    private bool TryStartRetreat(Entity<RMCGiantLizardComponent> ent, EntityUid target, float chance)
    {
        if (ActorQuery.HasComp(ent.Owner) ||
            ent.Comp.Retreating ||
            ent.Comp.Leaping ||
            ent.Comp.RavageTarget != null ||
            ent.Comp.NextRetreatAt > Timing.CurTime ||
            IsOnFire(ent.Owner) ||
            !ValidLivingMob(target) ||
            Faction.IsEntityFriendly(ent.Owner, target) ||
            !Random.Prob(chance))
        {
            return false;
        }

        WakeRest(ent);
        if (ent.Comp.Skirmishing)
            StopSkirmish(ent);

        if (ent.Comp.FoodTarget != null || ent.Comp.EatingFood)
            LoseFoodTarget(ent);

        ent.Comp.Retreating = true;
        ent.Comp.RetreatTarget = target;
        ent.Comp.RetreatUntil = Timing.CurTime + ent.Comp.RetreatDuration;
        ent.Comp.NextRetreatAt = ent.Comp.RetreatUntil + ent.Comp.RetreatCooldown;
        ent.Comp.NextRetreatMoveAt = TimeSpan.Zero;
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-retreats", ("lizard", ent.Owner), ("target", target)), ent.Owner, PopupType.MediumCaution);

        return true;
    }

    private bool UpdateRetreat(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (!ent.Comp1.Retreating)
            return false;

        if (ent.Comp1.RetreatUntil <= Timing.CurTime)
        {
            StopRetreat((ent.Owner, ent.Comp1));
            return false;
        }

        if (ent.Comp1.NextRetreatMoveAt > Timing.CurTime)
            return true;

        ent.Comp1.NextRetreatMoveAt = Timing.CurTime + ent.Comp1.RetreatRepathCooldown;

        if (ent.Comp1.RetreatTarget is { } target &&
            ValidLivingMob(target) &&
            XformQuery.HasComp(target))
        {
            TryMoveAwayFrom(ent.Owner, Transform.GetMoverCoordinates(target), ent.Comp1.RetreatSpeed);
        }
        else
        {
            TryMoveRandomly(ent.Owner, ent.Comp1.RetreatSpeed);
        }

        TryBreakNearbyObstacle(ent);
        return true;
    }

    private void StopRetreat(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.Retreating = false;
        ent.Comp.RetreatTarget = null;
        StopMovement(ent.Owner);
    }

    private bool TryAiFeed(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (ent.Comp1.EatingFood)
            return UpdateEatingFood((ent.Owner, ent.Comp1));

        EntityUid foodTarget;
        if (ent.Comp1.FoodTarget is { } existingFoodTarget)
        {
            foodTarget = existingFoodTarget;
        }
        else
        {
            if (ent.Comp1.NextFoodSearchAt > Timing.CurTime)
                return false;

            var pickedFood = PickFoodTarget(ent);
            if (pickedFood == null)
                return false;

            foodTarget = pickedFood.Value;
            ent.Comp1.FoodTarget = foodTarget;
        }

        if (!IsValidFoodTarget(foodTarget))
        {
            LoseFoodTarget((ent.Owner, ent.Comp1));
            return true;
        }

        if (TryHandleFoodHolder((ent.Owner, ent.Comp1), foodTarget))
            return true;

        var lizardCoords = Transform.GetMoverCoordinates(ent.Owner);
        var targetCoords = Transform.GetMoverCoordinates(foodTarget);
        if (!lizardCoords.TryDistance(EntityManager, targetCoords, out var targetDistance) ||
            targetDistance > ent.Comp1.FoodTargetKeepRange)
        {
            LoseFoodTarget((ent.Owner, ent.Comp1));
            return true;
        }

        if (targetDistance > ent.Comp1.AiFeedRange)
        {
            WakeRest((ent.Owner, ent.Comp1));
            TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.ForageSpeed);
            return true;
        }

        StartEatingFood((ent.Owner, ent.Comp1), foodTarget);
        return true;
    }

    private EntityUid? PickFoodTarget(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? bestFood = null;
        var bestDistance = float.MaxValue;

        foreach (var food in Lookup.GetEntitiesInRange<FoodComponent>(mapCoords, ent.Comp1.FoodSearchRange))
        {
            if (!IsAcceptedLizardFood(food.Owner) || !XformQuery.TryGetComponent(food.Owner, out var foodXform))
                continue;

            var foodCoords = Transform.GetMapCoordinates((food.Owner, foodXform));
            var distance = (foodCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            bestFood = food.Owner;
            bestDistance = distance;
        }

        return bestFood;
    }

    private void StartEatingFood(Entity<RMCGiantLizardComponent> ent, EntityUid food)
    {
        WakeRest(ent);
        StopMovement(ent.Owner);
        ent.Comp.EatingFood = true;
        ent.Comp.FoodBitesLeft = Random.Next(ent.Comp.FoodBitesMin, ent.Comp.FoodBitesMax + 1);
        ent.Comp.NextFoodBiteAt = Timing.CurTime + RandomTime(ent.Comp.FoodBiteDelayMin, ent.Comp.FoodBiteDelayMax);
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-starts-gnawing", ("lizard", ent.Owner), ("food", food)), ent.Owner);
    }

    private bool UpdateEatingFood(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.FoodTarget is not { } food || !IsValidFoodTarget(food))
        {
            LoseFoodTarget(ent);
            return true;
        }

        if (TryHandleFoodHolder(ent, food))
            return true;

        var lizardCoords = Transform.GetMoverCoordinates(ent.Owner);
        var targetCoords = Transform.GetMoverCoordinates(food);
        if (!lizardCoords.TryDistance(EntityManager, targetCoords, out var distance) ||
            distance > ent.Comp.AiFeedRange ||
            !MobState.IsAlive(ent.Owner))
        {
            LoseFoodTarget(ent);
            return true;
        }

        StopMovement(ent.Owner);
        if (ent.Comp.NextFoodBiteAt > Timing.CurTime)
            return true;

        ent.Comp.FoodBitesLeft--;
        _audio.PlayPvs(ent.Comp.EatingSound, ent.Owner);

        if (ent.Comp.FoodBitesLeft > 0)
        {
            ent.Comp.NextFoodBiteAt = Timing.CurTime + RandomTime(ent.Comp.FoodBiteDelayMin, ent.Comp.FoodBiteDelayMax);
            return true;
        }

        FinishEatingFood(ent, food);
        return true;
    }

    private void FinishEatingFood(Entity<RMCGiantLizardComponent> ent, EntityUid food)
    {
        if (_lastFoodHolder.TryGetValue(food, out var feeder) &&
            ValidLivingMob(feeder) &&
            Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(feeder), out var feederDistance) &&
            feederDistance <= ent.Comp.AiFeedTameRange &&
            !Faction.IsEntityFriendly(ent.Owner, feeder))
        {
            Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-stares-curiously", ("lizard", ent.Owner), ("user", feeder)), ent.Owner);
            TryTameToFeeder(ent.Owner, feeder, ent.Comp);
        }

        HealFraction(ent.Owner, ent.Comp.AiFeedHealFraction);
        QueueDel(food);
        ent.Comp.FoodTarget = null;
        ent.Comp.EatingFood = false;
        ent.Comp.FoodBitesLeft = 0;
        ent.Comp.NextFoodSearchAt = Timing.CurTime + ent.Comp.FoodEatenCooldown;
        UpdateLizardVisuals(ent);
    }

    private void LoseFoodTarget(Entity<RMCGiantLizardComponent> ent)
    {
        StopMovement(ent.Owner);
        ent.Comp.FoodTarget = null;
        ent.Comp.EatingFood = false;
        ent.Comp.FoodBitesLeft = 0;
        ent.Comp.NextFoodSearchAt = Timing.CurTime + ent.Comp.FoodLostCooldown;
    }

    private bool TryHandleFoodHolder(Entity<RMCGiantLizardComponent> ent, EntityUid food)
    {
        if (!TryGetFoodHolder(food, out var holder))
            return false;

        LoseFoodTarget(ent);
        PlayGrowl(ent);

        if (!ValidLivingMob(holder) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(holder), out var distance))
        {
            return true;
        }

        if (distance <= ent.Comp.FoodTheftRetaliateRange && !Faction.IsEntityFriendly(ent.Owner, holder))
        {
            TryAggro(ent.Owner, holder, ent.Comp);
            AlertPack(ent.Owner, holder, ent.Comp);
            Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-food-stolen", ("lizard", ent.Owner), ("user", holder)), ent.Owner, holder, PopupType.MediumCaution);
            return true;
        }

        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-growls-at", ("lizard", ent.Owner), ("user", holder)), ent.Owner, holder, PopupType.MediumCaution);
        return true;
    }

    private bool TryGetFoodHolder(EntityUid food, out EntityUid holder)
    {
        holder = default;
        if (!Container.TryGetContainingContainer((food, null, null), out var container))
            return false;

        holder = container.Owner;
        return MobQuery.HasComp(holder);
    }

    private bool IsValidFoodTarget(EntityUid food)
    {
        return !TerminatingOrDeleted(food) && IsAcceptedLizardFood(food);
    }

    private void TryRest(Entity<RMCGiantLizardComponent> ent)
    {
        if (Timing.CurTime - ent.Comp.LastHitAt < ent.Comp.CalmRestDelay)
            return;

        if (IsOnFire(ent.Owner))
            return;

        StopMovement(ent.Owner);
        if (!ent.Comp.SleepingForRest)
        {
            RMCNpc.SleepNPC(ent.Owner);
            ent.Comp.SleepingForRest = true;
        }

        ent.Comp.Resting = true;
        HealFraction(ent.Owner, ent.Comp.RestHealFraction);
        UpdateLizardVisuals(ent);
    }

    private void WakeRest(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.Resting = false;
        if (!ent.Comp.SleepingForRest)
            return;

        RMCNpc.WakeNPC(ent.Owner);
        ent.Comp.SleepingForRest = false;
        UpdateLizardVisuals(ent);
    }

    private bool TryFirePanic(Entity<RMCGiantLizardComponent> ent)
    {
        if (!FlammableQuery.TryComp(ent.Owner, out var flammable) || !flammable.OnFire)
            return false;

        WakeRest(ent);

        if (ent.Comp.NextFirePanicAt > Timing.CurTime)
            return true;

        ent.Comp.NextFirePanicAt = Timing.CurTime + ent.Comp.FirePanicCooldown;

        if (Random.Prob(ent.Comp.FireExtinguishChance))
            _rmcFlammable.Extinguish((ent.Owner, flammable));

        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);

        if (PhysicsQuery.TryComp(ent.Owner, out var physics))
            Physics.SetLinearVelocity(ent.Owner, Random.NextAngle().RotateVec(Vector2.UnitX) * ent.Comp.FirePanicSpeed, body: physics);

        return true;
    }

    private void UpdateTongueFlick(Entity<RMCGiantLizardComponent> ent)
    {
        var now = Timing.CurTime;
        if (ent.Comp.TongueVisible && ent.Comp.TongueFlickEndAt <= now)
        {
            ent.Comp.TongueVisible = false;
            _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Tongue, false);
        }

        if (ent.Comp.NextTongueFlickAt > now ||
            ent.Comp.Resting ||
            _standing.IsDown(ent.Owner) ||
            !MobState.IsAlive(ent.Owner))
        {
            return;
        }

        ent.Comp.NextTongueFlickAt = now + ent.Comp.TongueFlickCooldown;
        if (!Random.Prob(ent.Comp.TongueFlickChance))
            return;

        ShowTongueFlick(ent);
    }

    private void ShowTongueFlick(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.NextTongueFlickAt = Timing.CurTime + ent.Comp.TongueFlickCooldown;
        ent.Comp.TongueVisible = true;
        ent.Comp.TongueFlickEndAt = Timing.CurTime + ent.Comp.TongueFlickDuration;
        _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Tongue, true);
    }

    private void UpdateLizardVisuals(Entity<RMCGiantLizardComponent> ent)
    {
        var body = RMCGiantLizardBodyVisual.Running;
        if (!MobState.IsAlive(ent.Owner))
            body = RMCGiantLizardBodyVisual.Dead;
        else if (_standing.IsDown(ent.Owner))
            body = ent.Comp.Resting ? RMCGiantLizardBodyVisual.Sleeping : RMCGiantLizardBodyVisual.KnockedDown;
        else if (ent.Comp.Resting)
            body = RMCGiantLizardBodyVisual.Sleeping;

        _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Body, body);
        _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Wounds, GetWoundVisual(ent));
    }

    private RMCGiantLizardWoundVisual GetWoundVisual(Entity<RMCGiantLizardComponent> ent)
    {
        if (!MobState.IsAlive(ent.Owner) ||
            !DamageableQuery.TryComp(ent.Owner, out var damageable) ||
            !ThresholdsQuery.TryComp(ent.Owner, out var thresholds))
        {
            return RMCGiantLizardWoundVisual.None;
        }

        var maxHealth = 0f;
        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state == Content.Shared.Mobs.MobState.Dead && threshold > 0)
            {
                maxHealth = threshold.Float();
                break;
            }
        }

        if (maxHealth <= 0)
            return RMCGiantLizardWoundVisual.None;

        var healthFraction = 1f - damageable.Damage.GetTotal().Float() / maxHealth;
        if (healthFraction > ent.Comp.SmallWoundHealthFraction)
            return RMCGiantLizardWoundVisual.None;

        var big = healthFraction <= ent.Comp.BigWoundHealthFraction;
        if (_standing.IsDown(ent.Owner) && !ent.Comp.Resting)
            return big ? RMCGiantLizardWoundVisual.BigStun : RMCGiantLizardWoundVisual.SmallStun;

        if (ent.Comp.Resting)
            return big ? RMCGiantLizardWoundVisual.BigRest : RMCGiantLizardWoundVisual.SmallRest;

        return big ? RMCGiantLizardWoundVisual.Big : RMCGiantLizardWoundVisual.Small;
    }

    private void PlayGrowl(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.NextGrowlAt > Timing.CurTime)
            return;

        ent.Comp.NextGrowlAt = Timing.CurTime + RandomTime(ent.Comp.GrowlCooldownMin, ent.Comp.GrowlCooldownMax);
        _audio.PlayPvs(ent.Comp.GrowlSound, ent.Owner);
    }

    private string PickFriendlyPetPopup()
    {
        return Random.Pick(new[]
        {
            "rmc-giant-lizard-pet-happy",
            "rmc-giant-lizard-pet-nuzzle",
            "rmc-giant-lizard-pet-lick",
            "rmc-giant-lizard-pet-stare",
        });
    }

    private void TryBreakNearbyObstacle(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (ent.Comp1.NextObstacleAttackAt > Timing.CurTime)
            return;

        var coords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        foreach (var obstacle in Lookup.GetEntitiesInRange<DamageableComponent>(coords, 1.25f))
        {
            if (obstacle.Owner == ent.Owner ||
                MobQuery.HasComp(obstacle.Owner) ||
                ItemQuery.HasComp(obstacle.Owner) ||
                !XformQuery.TryGetComponent(obstacle.Owner, out var obstacleXform) ||
                !obstacleXform.Anchored)
            {
                continue;
            }

            Damageable.TryChangeDamage(obstacle.Owner, ent.Comp1.ObstacleDamage, origin: ent.Owner, tool: ent.Owner);
            ent.Comp1.NextObstacleAttackAt = Timing.CurTime + ent.Comp1.ObstacleAttackCooldown;
            return;
        }
    }

    private void HealFraction(EntityUid uid, float fraction)
    {
        if (!DamageableQuery.TryComp(uid, out var damageable))
            return;

        if (damageable.Damage.GetTotal() <= 0)
            return;

        Damageable.TryChangeDamage(
            uid,
            -(damageable.Damage * Math.Clamp(fraction, 0f, 1f)),
            true,
            interruptsDoAfters: false,
            origin: uid);
    }

    private bool IsAcceptedLizardFood(EntityUid food)
    {
        if (!HasComp<FoodComponent>(food))
            return false;

        if (Tags.HasAnyTag(food, "Meat"))
            return true;

        var proto = MetaData(food).EntityPrototype?.ID;
        if (proto == null)
            return false;

        return proto.Contains("Meat", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("MRE", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("PreparedMeal", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("Protein", StringComparison.OrdinalIgnoreCase) ||
               proto.Contains("ResinFruit", StringComparison.OrdinalIgnoreCase);
    }

    private void TryTameToFeeder(EntityUid lizard, EntityUid feeder, RMCGiantLizardComponent comp)
    {
        if (!FactionQuery.TryComp(feeder, out var feederFactions))
            return;

        foreach (var faction in feederFactions.Factions)
        {
            if (comp.ExcludedTameFactions.Contains(faction) || !comp.AllowedTameFactions.Contains(faction))
                continue;

            Faction.AddFaction(lizard, faction);
        }

        Faction.IgnoreEntity(lizard, feeder);
    }

    private void AlertPack(EntityUid lizard, EntityUid target, RMCGiantLizardComponent comp)
    {
        var coords = Transform.GetMapCoordinates(lizard);
        foreach (var ally in Lookup.GetEntitiesInRange<RMCGiantLizardComponent>(coords, comp.PackAlertRange))
        {
            if (ally.Owner == lizard || !ValidLivingMob(ally.Owner))
                continue;

            TryAggro(ally.Owner, target, ally.Comp);
        }
    }

    private void TryAggro(EntityUid uid, EntityUid target, RMCGiantLizardComponent comp)
    {
        if (!ValidLivingMob(target) || Faction.IsEntityFriendly(uid, target))
            return;

        comp.LastAggroAt = Timing.CurTime;
        Faction.AggroEntity(uid, target);
    }

    private void DecayAggression(Entity<RMCGiantLizardComponent> ent)
    {
        if (Timing.CurTime - ent.Comp.LastAggroAt <= ent.Comp.AggressionMemory)
            return;

        foreach (var hostile in Faction.GetHostiles(ent.Owner).ToArray())
        {
            Faction.DeAggroEntity(ent.Owner, hostile);
        }
    }

    private bool IsLowHealth(EntityUid uid, RMCGiantLizardComponent comp)
    {
        return TryGetHealthFraction(uid, comp, out var healthFraction) &&
               healthFraction <= comp.LowHealthRetreatFraction;
    }

    private bool TryGetHealthFraction(EntityUid uid, RMCGiantLizardComponent comp, out float healthFraction)
    {
        healthFraction = 1f;
        if (!DamageableQuery.TryComp(uid, out var damageable) ||
            !ThresholdsQuery.TryComp(uid, out var thresholds))
        {
            return false;
        }

        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state != Content.Shared.Mobs.MobState.Dead || threshold <= 0)
                continue;

            healthFraction = Math.Clamp(1f - damageable.Damage.GetTotal().Float() / threshold.Float(), 0f, 1f);
            return true;
        }

        return false;
    }
}
