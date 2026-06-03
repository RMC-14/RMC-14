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

public abstract class RMCAnimalSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;
    [Dependency] protected readonly NpcFactionSystem Faction = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly RMCNPCSystem RMCNpc = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly SharedStunSystem Stun = default!;
    [Dependency] protected readonly TagSystem Tags = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem Transform = default!;

    protected EntityQuery<ActorComponent> ActorQuery;
    protected EntityQuery<DamageableComponent> DamageableQuery;
    protected EntityQuery<FlammableComponent> FlammableQuery;
    protected EntityQuery<ItemComponent> ItemQuery;
    protected EntityQuery<MobStateComponent> MobQuery;
    protected EntityQuery<MobThresholdsComponent> ThresholdsQuery;
    protected EntityQuery<NpcFactionMemberComponent> FactionQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;
    protected EntityQuery<TransformComponent> XformQuery;

    public override void Initialize()
    {
        base.Initialize();

        ActorQuery = GetEntityQuery<ActorComponent>();
        DamageableQuery = GetEntityQuery<DamageableComponent>();
        FlammableQuery = GetEntityQuery<FlammableComponent>();
        ItemQuery = GetEntityQuery<ItemComponent>();
        MobQuery = GetEntityQuery<MobStateComponent>();
        ThresholdsQuery = GetEntityQuery<MobThresholdsComponent>();
        FactionQuery = GetEntityQuery<NpcFactionMemberComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        XformQuery = GetEntityQuery<TransformComponent>();
    }

    protected TimeSpan RandomTime(TimeSpan min, TimeSpan max)
    {
        if (max <= min)
            return min;

        return min + TimeSpan.FromSeconds(Random.NextDouble() * (max - min).TotalSeconds);
    }

    protected bool ValidLivingMob(EntityUid uid)
    {
        return !TerminatingOrDeleted(uid) &&
               MobQuery.HasComp(uid) &&
               MobState.IsAlive(uid);
    }

    protected bool IsOnFire(EntityUid uid)
    {
        return FlammableQuery.TryComp(uid, out var flammable) && flammable.OnFire;
    }

    protected void StopMovement(EntityUid uid)
    {
        if (PhysicsQuery.TryComp(uid, out var physics))
            Physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
    }

    protected bool TryMoveTowards(EntityUid uid, EntityCoordinates target, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var origin = Transform.GetMoverCoordinates(uid);
        if (!origin.TryDistance(EntityManager, target, out _))
            return false;

        var direction = target.Position - origin.Position;
        if (direction.LengthSquared() < 0.01f)
            return false;

        Physics.SetLinearVelocity(uid, direction.Normalized() * speed, body: physics);
        return true;
    }

    protected bool TryMoveAwayFrom(EntityUid uid, EntityCoordinates target, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var origin = Transform.GetMoverCoordinates(uid);
        if (!origin.TryDistance(EntityManager, target, out _))
            return false;

        var direction = origin.Position - target.Position;
        if (direction.LengthSquared() < 0.01f)
            direction = Random.NextAngle().RotateVec(Vector2.UnitX);

        Physics.SetLinearVelocity(uid, direction.Normalized() * speed, body: physics);
        return true;
    }

    protected bool TryMoveRandomly(EntityUid uid, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var direction = Random.NextAngle().RotateVec(Vector2.UnitX);
        Physics.SetLinearVelocity(uid, direction * speed, body: physics);
        return true;
    }

    protected int CountNearby<T>(MapCoordinates coordinates, float range) where T : IComponent
    {
        var count = 0;
        foreach (var ent in Lookup.GetEntitiesInRange<T>(coordinates, range))
        {
            if (ent.Owner.Valid)
                count++;
        }

        return count;
    }

    protected void SpawnNear(EntProtoId prototype, EntityCoordinates coordinates, float radius)
    {
        var offset = Random.NextAngle().RotateVec(Vector2.UnitX) * Random.NextFloat(0f, radius);
        Spawn(prototype, coordinates.Offset(offset));
    }
}

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

            if (TryRetreat(ent, target.Value))
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

    private bool TryRetreat(Entity<RMCGiantLizardComponent, TransformComponent> ent, EntityUid target)
    {
        if (!IsLowHealth(ent.Owner, ent.Comp1))
            return false;

        WakeRest((ent.Owner, ent.Comp1));
        Faction.DeAggroEntity(ent.Owner, target);

        if (!XformQuery.TryGetComponent(target, out _))
            return true;

        var lizardCoords = Transform.GetMoverCoordinates(ent.Owner);
        var targetCoords = Transform.GetMoverCoordinates(target);
        if (!lizardCoords.TryDistance(EntityManager, targetCoords, out _))
            return true;

        var away = lizardCoords.Position - targetCoords.Position;
        if (away.LengthSquared() < 0.01f)
            away = Random.NextAngle().RotateVec(Vector2.UnitX);

        if (PhysicsQuery.TryComp(ent.Owner, out var physics))
            Physics.SetLinearVelocity(ent.Owner, away.Normalized() * ent.Comp1.FirePanicSpeed, body: physics);

        return true;
    }

    private bool TryAiFeed(Entity<RMCGiantLizardComponent, TransformComponent> ent)
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

        if (bestFood == null)
            return false;

        var targetCoords = Transform.GetMoverCoordinates(bestFood.Value);
        if (bestDistance > ent.Comp1.AiFeedRange)
        {
            WakeRest((ent.Owner, ent.Comp1));
            TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.ForageSpeed);
            return true;
        }

        HealFraction(ent.Owner, ent.Comp1.AiFeedHealFraction);
        QueueDel(bestFood.Value);
        return true;
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
        if (!DamageableQuery.TryComp(uid, out var damageable) ||
            !ThresholdsQuery.TryComp(uid, out var thresholds))
        {
            return false;
        }

        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state != Content.Shared.Mobs.MobState.Dead || threshold <= 0)
                continue;

            var healthFraction = 1f - damageable.Damage.GetTotal().Float() / threshold.Float();
            return healthFraction <= comp.LowHealthRetreatFraction;
        }

        return false;
    }
}

public sealed class RMCBatSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBatHangingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCBatHangingComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCBatHangingComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextCheckAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.CheckCooldown);
    }

    private void OnDamageChanged(Entity<RMCBatHangingComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            WakeBat(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCBatHangingComponent>();
        while (query.MoveNext(out var uid, out var bat))
        {
            if (bat.NextCheckAt > now)
                continue;

            bat.NextCheckAt = now + bat.CheckCooldown;

            if (ActorQuery.HasComp(uid) || !MobState.IsAlive(uid))
            {
                WakeBat((uid, bat));
                continue;
            }

            if (bat.Hanging)
            {
                if (Random.Prob(bat.WakeChance) || TryWakeFromDisturbance((uid, bat)))
                    WakeBat((uid, bat));
            }
            else if (Random.Prob(bat.HangChance) && CanHang(uid, bat))
            {
                HangBat((uid, bat));
            }
        }
    }

    private bool TryWakeFromDisturbance(Entity<RMCBatHangingComponent> ent)
    {
        if (ent.Comp.DisturbanceRange <= 0)
            return false;

        var coords = Transform.GetMapCoordinates(ent.Owner);
        foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(coords, ent.Comp.DisturbanceRange))
        {
            if (mob.Owner == ent.Owner ||
                HasComp<RMCBatHangingComponent>(mob.Owner) ||
                !MobState.IsAlive(mob.Owner, mob.Comp))
            {
                continue;
            }

            if (!Random.Prob(ent.Comp.DisturbanceWakeChance))
                return false;

            Popup.PopupEntity(Loc.GetString("rmc-bat-wakes-disturbed", ("bat", ent.Owner)), ent.Owner);
            return true;
        }

        return false;
    }

    private bool CanHang(EntityUid uid, RMCBatHangingComponent comp)
    {
        if (!comp.RequireBlockedNorth)
            return true;

        var coords = Transform(uid).Coordinates.Offset(Direction.North.ToVec());
        return _turf.TryGetTileRef(coords, out var tile) &&
               _turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable);
    }

    private void HangBat(Entity<RMCBatHangingComponent> ent)
    {
        ent.Comp.Hanging = true;
        StopMovement(ent.Owner);
        _appearance.SetData(ent.Owner, RMCBatVisuals.Hanging, true);
        RMCNpc.SleepNPC(ent.Owner);
    }

    private void WakeBat(Entity<RMCBatHangingComponent> ent)
    {
        if (!ent.Comp.Hanging)
            return;

        ent.Comp.Hanging = false;
        _appearance.SetData(ent.Owner, RMCBatVisuals.Hanging, false);
        RMCNpc.WakeNPC(ent.Owner);
    }
}

public sealed class RMCSpiderColonySystem : RMCAnimalSystem
{
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSpiderNestMemberComponent, MapInitEvent>(OnSpiderNestMemberMapInit);
        SubscribeLocalEvent<RMCSpiderNestMemberComponent, MobStateChangedEvent>(OnSpiderNestMemberMobStateChanged);
        SubscribeLocalEvent<RMCSpiderVenomComponent, MeleeHitEvent>(OnSpiderMeleeHit);
        SubscribeLocalEvent<RMCSpiderWebComponent, PreventCollideEvent>(OnWebPreventCollide);
        SubscribeLocalEvent<RMCSpiderWebComponent, StartCollideEvent>(OnWebStartCollide);
        SubscribeLocalEvent<RMCSpiderNurseComponent, DamageChangedEvent>(OnNurseDamageChanged);
        SubscribeLocalEvent<RMCSpiderNurseComponent, MobStateChangedEvent>(OnNurseMobStateChanged);
        SubscribeLocalEvent<RMCSpiderEggComponent, MapInitEvent>(OnEggMapInit);
        SubscribeLocalEvent<RMCSpiderlingGrowthComponent, MapInitEvent>(OnSpiderlingMapInit);
        SubscribeLocalEvent<RMCSpiderlingGrowthComponent, MobStateChangedEvent>(OnSpiderlingMobStateChanged);
        SubscribeLocalEvent<RMCSpiderCocoonComponent, ComponentInit>(OnCocoonInit);
        SubscribeLocalEvent<RMCSpiderCocoonComponent, ComponentShutdown>(OnCocoonShutdown);
    }

    private void OnSpiderNestMemberMapInit(Entity<RMCSpiderNestMemberComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextIdleSkitterAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.IdleSkitterCooldown);
    }

    private void OnSpiderNestMemberMobStateChanged(Entity<RMCSpiderNestMemberComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Alive)
            return;

        StopAdultSpiderSkitter(ent);
    }

    private void OnSpiderMeleeHit(Entity<RMCSpiderVenomComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!ValidLivingMob(target) || HasComp<RMCSpiderNestMemberComponent>(target))
                continue;

            if (Random.Prob(ent.Comp.PrickPopupChance))
                Popup.PopupEntity(Loc.GetString("rmc-spider-venom-prick"), target, target, PopupType.SmallCaution);

            if (ent.Comp.DazeTime > TimeSpan.Zero)
                _dazed.TryDaze(target, ent.Comp.DazeTime, true);
        }
    }

    private void OnWebPreventCollide(Entity<RMCSpiderWebComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<RMCSpiderNestMemberComponent>(args.OtherEntity) || HasComp<IgnoreSpiderWebComponent>(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (!HasComp<ProjectileComponent>(args.OtherEntity))
            return;

        if (!Random.Prob(ent.Comp.ProjectileBlockChance))
            args.Cancelled = true;
    }

    private void OnWebStartCollide(Entity<RMCSpiderWebComponent> ent, ref StartCollideEvent args)
    {
        if (HasComp<RMCSpiderNestMemberComponent>(args.OtherEntity) ||
            HasComp<IgnoreSpiderWebComponent>(args.OtherEntity) ||
            !MobQuery.HasComp(args.OtherEntity) ||
            !Random.Prob(ent.Comp.MobRootChance))
        {
            return;
        }

        _slow.TryRoot(args.OtherEntity, ent.Comp.MobRootTime);
        Stun.TrySlowdown(args.OtherEntity, ent.Comp.MobRootTime, true, 0.2f, 0.2f);
    }

    private void OnNurseDamageChanged(Entity<RMCSpiderNurseComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        CancelNurseWork(ent, true);
    }

    private void OnNurseMobStateChanged(Entity<RMCSpiderNurseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Alive)
            return;

        CancelNurseWork(ent, false);
    }

    private void OnEggMapInit(Entity<RMCSpiderEggComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.HatchAt = Timing.CurTime + RandomTime(ent.Comp.HatchMin, ent.Comp.HatchMax);
    }

    private void OnSpiderlingMapInit(Entity<RMCSpiderlingGrowthComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.GrowAt = Timing.CurTime + RandomTime(ent.Comp.GrowMin, ent.Comp.GrowMax);
        ent.Comp.NextSkitterAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.SkitterCooldown);
    }

    private void OnSpiderlingMobStateChanged(Entity<RMCSpiderlingGrowthComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != Content.Shared.Mobs.MobState.Dead || ent.Comp.SpawnedRemains)
            return;

        ent.Comp.SpawnedRemains = true;
        if (XformQuery.TryGetComponent(ent.Owner, out var xform))
            Spawn(ent.Comp.RemainsPrototype, xform.Coordinates);
    }

    private void OnCocoonInit(Entity<RMCSpiderCocoonComponent> ent, ref ComponentInit args)
    {
        Container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
    }

    private void OnCocoonShutdown(Entity<RMCSpiderCocoonComponent> ent, ref ComponentShutdown args)
    {
        if (!Container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        EntityCoordinates? destination = null;
        if (XformQuery.TryGetComponent(ent.Owner, out var xform))
        {
            destination = xform.Coordinates;
            if (container.ContainedEntities.Count > 0)
                Popup.PopupEntity(Loc.GetString("rmc-spider-cocoon-splits", ("cocoon", ent.Owner)), ent.Owner);
        }

        Container.EmptyContainer(container, true, destination);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateAdultSpiderSkitter();
        UpdateNurses();
        UpdateEggs();
        UpdateSpiderlings();
    }

    private void UpdateAdultSpiderSkitter()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderNestMemberComponent>();
        while (query.MoveNext(out var uid, out var spider))
        {
            if (HasComp<RMCSpiderlingGrowthComponent>(uid))
                continue;

            if (spider.IdleSkittering)
            {
                if (ActorQuery.HasComp(uid) ||
                    !MobState.IsAlive(uid) ||
                    now >= spider.IdleSkitterUntil)
                {
                    StopAdultSpiderSkitter((uid, spider));
                }

                continue;
            }

            if (spider.NextIdleSkitterAt > now ||
                ActorQuery.HasComp(uid) ||
                !MobState.IsAlive(uid) ||
                HasBusyNurseWork(uid) ||
                !Random.Prob(spider.IdleSkitterChance))
            {
                continue;
            }

            StartAdultSpiderSkitter((uid, spider));
        }
    }

    private bool HasBusyNurseWork(EntityUid uid)
    {
        return TryComp<RMCSpiderNurseComponent>(uid, out var nurse) &&
               nurse.BusyWork != RMCSpiderNurseWork.None;
    }

    private void StartAdultSpiderSkitter(Entity<RMCSpiderNestMemberComponent> ent)
    {
        ent.Comp.IdleSkittering = true;
        ent.Comp.IdleSkitterUntil = Timing.CurTime + ent.Comp.IdleSkitterDuration;
        ent.Comp.NextIdleSkitterAt = ent.Comp.IdleSkitterUntil + ent.Comp.IdleSkitterCooldown;

        RMCNpc.SleepNPC(ent.Owner);
        TryMoveRandomly(ent.Owner, ent.Comp.IdleSkitterSpeed);
        Popup.PopupEntity(Loc.GetString("rmc-spider-skitters-madly", ("spider", ent.Owner)), ent.Owner);
    }

    private void StopAdultSpiderSkitter(Entity<RMCSpiderNestMemberComponent> ent)
    {
        if (!ent.Comp.IdleSkittering)
            return;

        ent.Comp.IdleSkittering = false;
        ent.Comp.IdleSkitterUntil = TimeSpan.Zero;
        StopMovement(ent.Owner);

        if (MobState.IsAlive(ent.Owner) && !ActorQuery.HasComp(ent.Owner))
            RMCNpc.WakeNPC(ent.Owner);
    }

    private void UpdateNurses()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderNurseComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var nurse, out var xform))
        {
            if (!MobState.IsAlive(uid))
            {
                ClearNurseWork(nurse);
                continue;
            }

            var ent = (uid, nurse, xform);
            var mapCoords = Transform.GetMapCoordinates((uid, xform));

            if (nurse.BusyWork != RMCSpiderNurseWork.None)
            {
                UpdateNurseWork(ent, mapCoords);
                continue;
            }

            if (nurse.NextThinkAt > now)
                continue;

            nurse.NextThinkAt = now + nurse.ThinkCooldown;

            if (!Random.Prob(nurse.IdleWorkChance))
                continue;

            TryStartNurseWork(ent, mapCoords);
        }
    }

    private bool TryStartNurseWork(Entity<RMCSpiderNurseComponent, TransformComponent> ent, MapCoordinates mapCoords)
    {
        var spiders = CountNearby<RMCSpiderNestMemberComponent>(mapCoords, ent.Comp1.NestRange);
        var eggs = CountNearby<RMCSpiderEggComponent>(mapCoords, ent.Comp1.NestRange);
        var webs = CountNearby<RMCSpiderWebComponent>(mapCoords, ent.Comp1.NestRange);
        var cocoons = CountNearby<RMCSpiderCocoonComponent>(mapCoords, ent.Comp1.NestRange);

        var livingTarget = PickCocoonTarget(ent.Owner, ent.Comp1, mapCoords, true, false);
        if (livingTarget != null &&
            cocoons < ent.Comp1.MaxCocoons &&
            TryStartCocoonTarget(ent, livingTarget.Value))
            return true;

        if (webs < ent.Comp1.MaxWebs && !TileHasWeb(ent.Comp2.Coordinates))
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.SpinWeb, null, ent.Comp1.WebSpinTime);
            return true;
        }

        if (ent.Comp1.Fed > 0 &&
            eggs < ent.Comp1.MaxEggs &&
            spiders < ent.Comp1.MaxActiveSpiders &&
            TileHasWeb(ent.Comp2.Coordinates))
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.LayEggs, null, ent.Comp1.EggLayTime);
            return true;
        }

        var itemTarget = PickCocoonTarget(ent.Owner, ent.Comp1, mapCoords, false, true);
        return itemTarget != null &&
               cocoons < ent.Comp1.MaxCocoons &&
               TryStartCocoonTarget(ent, itemTarget.Value);
    }

    private bool TryStartCocoonTarget(Entity<RMCSpiderNurseComponent, TransformComponent> ent, EntityUid target)
    {
        if (!XformQuery.TryGetComponent(target, out var targetXform) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, targetXform.Coordinates, out var distance))
        {
            return false;
        }

        if (distance > ent.Comp1.CocoonRange)
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.MovingToTarget, target, ent.Comp1.TargetGiveUpTime);
            TryMoveTowards(ent.Owner, targetXform.Coordinates, ent.Comp1.MoveToTargetSpeed);
            return true;
        }

        StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.Cocoon, target, ent.Comp1.CocoonSpinTime);
        return true;
    }

    private void UpdateNurseWork(Entity<RMCSpiderNurseComponent, TransformComponent> ent, MapCoordinates mapCoords)
    {
        if (ent.Comp1.BusyWork == RMCSpiderNurseWork.MovingToTarget)
        {
            UpdateNurseMoveToTarget(ent);
            return;
        }

        if (ent.Comp1.BusyUntil > Timing.CurTime)
            return;

        switch (ent.Comp1.BusyWork)
        {
            case RMCSpiderNurseWork.SpinWeb:
                if (CountNearby<RMCSpiderWebComponent>(mapCoords, ent.Comp1.NestRange) < ent.Comp1.MaxWebs &&
                    !TileHasWeb(ent.Comp2.Coordinates))
                {
                    Spawn(ent.Comp1.WebPrototype, ent.Comp2.Coordinates);
                    Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-finish-web", ("spider", ent.Owner)), ent.Owner);
                }
                break;
            case RMCSpiderNurseWork.LayEggs:
                if (ent.Comp1.Fed > 0 &&
                    CountNearby<RMCSpiderEggComponent>(mapCoords, ent.Comp1.NestRange) < ent.Comp1.MaxEggs &&
                    CountNearby<RMCSpiderNestMemberComponent>(mapCoords, ent.Comp1.NestRange) < ent.Comp1.MaxActiveSpiders)
                {
                    Spawn(ent.Comp1.EggPrototype, ent.Comp2.Coordinates);
                    ent.Comp1.Fed--;
                    Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-finish-eggs", ("spider", ent.Owner)), ent.Owner);
                }
                break;
            case RMCSpiderNurseWork.Cocoon:
                TryCreateCocoon(ent.Owner, ent.Comp1, mapCoords);
                break;
        }

        ClearNurseWork(ent.Comp1);
    }

    private void UpdateNurseMoveToTarget(Entity<RMCSpiderNurseComponent, TransformComponent> ent)
    {
        var target = ent.Comp1.WorkTarget;
        if (target == null ||
            TerminatingOrDeleted(target.Value) ||
            !XformQuery.TryGetComponent(target.Value, out var targetXform))
        {
            ClearNurseWork(ent.Comp1);
            return;
        }

        var targetCoords = targetXform.Coordinates;
        if (!Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, targetCoords, out var distance))
        {
            ClearNurseWork(ent.Comp1);
            return;
        }

        if (distance <= ent.Comp1.CocoonRange)
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.Cocoon, target, ent.Comp1.CocoonSpinTime);
            return;
        }

        if (ent.Comp1.BusyUntil <= Timing.CurTime)
        {
            Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-gives-up", ("spider", ent.Owner), ("target", target.Value)), ent.Owner);
            ClearNurseWork(ent.Comp1);
            return;
        }

        TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.MoveToTargetSpeed);
    }

    private void StartNurseWork(Entity<RMCSpiderNurseComponent> ent, RMCSpiderNurseWork work, EntityUid? target, TimeSpan duration)
    {
        ent.Comp.BusyWork = work;
        ent.Comp.WorkTarget = target;
        ent.Comp.BusyUntil = Timing.CurTime + duration;
        ent.Comp.TargetAcquiredAt = Timing.CurTime;

        if (work != RMCSpiderNurseWork.MovingToTarget)
            StopMovement(ent.Owner);

        switch (work)
        {
            case RMCSpiderNurseWork.SpinWeb:
                Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-start-web", ("spider", ent.Owner)), ent.Owner);
                break;
            case RMCSpiderNurseWork.LayEggs:
                Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-start-eggs", ("spider", ent.Owner)), ent.Owner);
                break;
            case RMCSpiderNurseWork.Cocoon when target != null:
                Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-start-cocoon", ("spider", ent.Owner), ("target", target.Value)), ent.Owner);
                break;
        }
    }

    private void CancelNurseWork(Entity<RMCSpiderNurseComponent> ent, bool popup)
    {
        if (ent.Comp.BusyWork == RMCSpiderNurseWork.None)
            return;

        if (popup)
            Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-work-interrupted", ("spider", ent.Owner)), ent.Owner);

        ClearNurseWork(ent.Comp);
    }

    private static void ClearNurseWork(RMCSpiderNurseComponent nurse)
    {
        nurse.BusyWork = RMCSpiderNurseWork.None;
        nurse.WorkTarget = null;
        nurse.BusyUntil = TimeSpan.Zero;
    }

    private void UpdateEggs()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderEggComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var egg, out var xform))
        {
            if (egg.HatchAt > now)
                continue;

            var mapCoords = Transform.GetMapCoordinates((uid, xform));
            var activeSpiders = CountNearby<RMCSpiderNestMemberComponent>(mapCoords, egg.NestRange);
            var remaining = Math.Max(0, egg.MaxActiveSpiders - activeSpiders);
            if (remaining <= 0)
            {
                egg.HatchAt = now + RandomTime(egg.HatchMin, egg.HatchMax);
                continue;
            }

            var amount = Math.Min(Random.Next(egg.MinSpawned, egg.MaxSpawned + 1), remaining);
            for (var i = 0; i < amount; i++)
                SpawnNear(egg.SpawnPrototype, xform.Coordinates, 1.25f);

            Popup.PopupEntity(Loc.GetString("rmc-spider-egg-hatches", ("egg", uid)), uid);
            QueueDel(uid);
        }
    }

    private void UpdateSpiderlings()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderlingGrowthComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spiderling, out var xform))
        {
            UpdateSpiderlingSkitter((uid, spiderling, xform), now);

            if (spiderling.GrowAt > now)
                continue;

            if (!spiderling.NoGrow && Random.Prob(spiderling.GrowChance))
            {
                Spawn(PickSpiderMaturePrototype(spiderling), xform.Coordinates);
                QueueDel(uid);
                continue;
            }

            spiderling.GrowAt = now + RandomTime(spiderling.GrowMin, spiderling.GrowMax);
        }
    }

    private void UpdateSpiderlingSkitter(Entity<RMCSpiderlingGrowthComponent, TransformComponent> ent, TimeSpan now)
    {
        if (ent.Comp1.NextSkitterAt > now || !MobState.IsAlive(ent.Owner))
            return;

        ent.Comp1.NextSkitterAt = now + ent.Comp1.SkitterCooldown;

        if (Random.Prob(ent.Comp1.ChitterChance))
            Popup.PopupEntity(Loc.GetString("rmc-spiderling-chitters", ("spiderling", ent.Owner)), ent.Owner);

        if (!Random.Prob(ent.Comp1.SkitterChance))
        {
            TryMoveSpiderlingToVent(ent);
            return;
        }

        EntityUid? target = null;
        foreach (var candidate in Lookup.GetEntitiesInRange(ent.Owner, ent.Comp1.SkitterRange, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate))
        {
            if (candidate == ent.Owner || TerminatingOrDeleted(candidate))
                continue;

            target = candidate;
            break;
        }

        if (target == null || !XformQuery.TryGetComponent(target.Value, out var targetXform))
            return;

        TryMoveTowards(ent.Owner, targetXform.Coordinates, 4f);
        if (Random.Prob(0.25f))
            Popup.PopupEntity(Loc.GetString("rmc-spiderling-skitters", ("spiderling", ent.Owner)), ent.Owner);
    }

    private bool TryMoveSpiderlingToVent(Entity<RMCSpiderlingGrowthComponent, TransformComponent> ent)
    {
        if (!Random.Prob(ent.Comp1.VentSearchChance))
            return false;

        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var vent in Lookup.GetEntitiesInRange<VentEntranceComponent>(mapCoords, ent.Comp1.VentSearchRange))
        {
            if (!XformQuery.TryGetComponent(vent.Owner, out var ventXform))
                continue;

            var ventCoords = Transform.GetMapCoordinates((vent.Owner, ventXform));
            var distance = (ventCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = vent.Owner;
            bestDistance = distance;
        }

        if (best == null || !XformQuery.TryGetComponent(best.Value, out var targetXform))
            return false;

        TryMoveTowards(ent.Owner, targetXform.Coordinates, ent.Comp1.VentMoveSpeed);
        if (Random.Prob(0.25f))
            Popup.PopupEntity(Loc.GetString("rmc-spiderling-skitters-away", ("spiderling", ent.Owner)), ent.Owner);

        return true;
    }

    private bool TryCreateCocoon(EntityUid nurse, RMCSpiderNurseComponent comp, MapCoordinates mapCoords)
    {
        if (CountNearby<RMCSpiderCocoonComponent>(mapCoords, comp.NestRange) >= comp.MaxCocoons)
            return false;

        var target = comp.WorkTarget;
        if (target == null || !XformQuery.TryGetComponent(target.Value, out var targetXform))
            return false;

        if (!Transform.GetMoverCoordinates(nurse).TryDistance(EntityManager, targetXform.Coordinates, out var distance) ||
            distance > comp.CocoonRange)
        {
            return false;
        }

        var candidates = GetCocoonCandidates(nurse, target.Value, targetXform.Coordinates, 24);
        var valid = new List<(EntityUid Candidate, bool LivingPrey, bool LargeCocoon)>();
        var livingQueued = false;
        var largeCocoon = false;

        foreach (var candidate in candidates)
        {
            if (!CanCocoonCandidate(candidate, nurse, livingQueued, out var livingPrey, out var largeCandidate))
                continue;

            valid.Add((candidate, livingPrey, largeCandidate));
            livingQueued |= livingPrey;
            largeCocoon |= largeCandidate;
        }

        if (valid.Count == 0)
            return false;

        var cocoon = Spawn(largeCocoon ? comp.LargeCocoonPrototype : comp.CocoonPrototype, targetXform.Coordinates);
        if (!TryComp<RMCSpiderCocoonComponent>(cocoon, out var cocoonComp))
            return false;

        var container = Container.EnsureContainer<Container>(cocoon, cocoonComp.ContainerId);
        var inserted = 0;
        var fed = false;

        foreach (var candidate in valid)
        {
            if (container.ContainedEntities.Count >= cocoonComp.MaxContents)
                break;

            if (!Container.Insert((candidate.Candidate, null, null, null), container, force: true))
                continue;

            inserted++;
            fed |= candidate.LivingPrey;
        }

        if (inserted == 0)
        {
            QueueDel(cocoon);
            return false;
        }

        if (fed)
        {
            comp.Fed++;
            Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-cocoon-fed", ("spider", nurse), ("target", target.Value)), nurse);
        }

        Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-cocoon-created", ("spider", nurse), ("cocoon", cocoon)), nurse);

        return true;
    }

    private List<EntityUid> GetCocoonCandidates(EntityUid nurse, EntityUid target, EntityCoordinates coordinates, int maxContents)
    {
        var candidates = new List<EntityUid>(maxContents);
        var seen = new HashSet<EntityUid>();

        AddCandidate(target);

        foreach (var candidate in Lookup.GetEntitiesInRange(coordinates, 0.35f, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate))
        {
            if (candidates.Count >= maxContents)
                break;

            AddCandidate(candidate);
        }

        return candidates;

        void AddCandidate(EntityUid candidate)
        {
            if (candidate == nurse ||
                !seen.Add(candidate) ||
                TerminatingOrDeleted(candidate))
            {
                return;
            }

            candidates.Add(candidate);
        }
    }

    private bool CanCocoonCandidate(EntityUid candidate, EntityUid nurse, bool alreadyFed, out bool livingPrey, out bool largeCocoon)
    {
        livingPrey = false;
        largeCocoon = false;

        if (candidate == nurse ||
            HasComp<RMCSpiderNestMemberComponent>(candidate) ||
            HasComp<RMCSpiderCocoonComponent>(candidate) ||
            TerminatingOrDeleted(candidate) ||
            !XformQuery.TryGetComponent(candidate, out var xform) ||
            xform.Anchored)
        {
            return false;
        }

        if (MobQuery.TryComp(candidate, out var mob))
        {
            if (alreadyFed || !MobState.IsIncapacitated(candidate, mob))
                return false;

            livingPrey = true;
            largeCocoon = true;
            return true;
        }

        if (ItemQuery.HasComp(candidate))
            return true;

        largeCocoon = DamageableQuery.HasComp(candidate);
        return largeCocoon;
    }

    private EntityUid? PickCocoonTarget(EntityUid nurse, RMCSpiderNurseComponent comp, MapCoordinates mapCoords, bool includeLiving, bool includeItems)
    {
        if (includeLiving)
        {
            foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, comp.TargetSearchRange))
            {
                if (mob.Owner == nurse ||
                    HasComp<RMCSpiderNestMemberComponent>(mob.Owner) ||
                    !MobState.IsIncapacitated(mob.Owner, mob.Comp))
                {
                    continue;
                }

                comp.TargetAcquiredAt = Timing.CurTime;
                return mob.Owner;
            }
        }

        if (includeItems)
        {
            foreach (var item in Lookup.GetEntitiesInRange<ItemComponent>(mapCoords, comp.TargetSearchRange))
            {
                if (item.Owner == nurse || HasComp<RMCSpiderNestMemberComponent>(item.Owner))
                    continue;

                comp.TargetAcquiredAt = Timing.CurTime;
                return item.Owner;
            }
        }

        return null;
    }

    private bool TileHasWeb(EntityCoordinates coords)
    {
        foreach (var web in Lookup.GetEntitiesInRange<RMCSpiderWebComponent>(coords, 0.3f))
        {
            if (web.Owner.Valid)
                return true;
        }

        return false;
    }

    private EntProtoId PickSpiderMaturePrototype(RMCSpiderlingGrowthComponent comp)
    {
        var total = comp.GuardWeight + comp.HunterWeight + comp.NurseWeight;
        var roll = Random.NextFloat(0f, total);

        if (roll < comp.GuardWeight)
            return comp.GuardPrototype;

        if (roll < comp.GuardWeight + comp.HunterWeight)
            return comp.HunterPrototype;

        return comp.NursePrototype;
    }
}

public sealed class RMCSmallAnimalSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCTinyLizardComponent, InteractHandEvent>(OnTinyLizardInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCTinyLizardComponent, DisarmedEvent>(OnTinyLizardDisarmed);
        SubscribeLocalEvent<RMCTinyLizardComponent, DamageChangedEvent>(OnTinyLizardDamageChanged);
    }

    private void OnTinyLizardInteractHand(Entity<RMCTinyLizardComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-pet", ("user", args.User), ("lizard", ent.Owner)), ent.Owner);

        if (!Random.Prob(ent.Comp.HissChance))
            return;

        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-hisses", ("lizard", ent.Owner)), ent.Owner);
        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);
    }

    private void OnTinyLizardDisarmed(Entity<RMCTinyLizardComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-shoo", ("user", args.Source), ("lizard", ent.Owner)), ent.Owner);

        if (!XformQuery.HasComp(args.Source))
            return;

        _size.KnockBack(ent.Owner,
            Transform.GetMapCoordinates(args.Source),
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockbackSpeed,
            true);
    }

    private void OnTinyLizardDamageChanged(Entity<RMCTinyLizardComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.Origin is not { } origin ||
            origin == ent.Owner ||
            !ActorQuery.HasComp(origin) ||
            !MobQuery.HasComp(origin) ||
            ent.Comp.NextStompPopupAt > Timing.CurTime)
        {
            return;
        }

        var lizardCoords = Transform.GetMoverCoordinates(ent.Owner);
        var originCoords = Transform.GetMoverCoordinates(origin);
        if (!lizardCoords.TryDistance(EntityManager, originCoords, out var distance) || distance > 1.75f)
            return;

        ent.Comp.NextStompPopupAt = Timing.CurTime + ent.Comp.StompPopupCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-tiny-lizard-stomp", ("user", origin), ("lizard", ent.Owner)), ent.Owner);
    }
}

public sealed class RMCRodentSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCRodentBehaviorComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCRodentBehaviorComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnMapInit(Entity<RMCRodentBehaviorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        ent.Comp.SleepUntil = TimeSpan.Zero;
    }

    private void OnDamageChanged(Entity<RMCRodentBehaviorComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            WakeRodent(ent);
    }

    private void OnMobStateChanged(Entity<RMCRodentBehaviorComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != Content.Shared.Mobs.MobState.Alive)
            WakeRodent(ent);
    }

    private void OnStartCollide(Entity<RMCRodentBehaviorComponent> ent, ref StartCollideEvent args)
    {
        if (!MobState.IsAlive(ent.Owner))
            return;

        if (ent.Comp.Sleeping)
            WakeRodent(ent);

        if (ent.Comp.NextSqueakAt > Timing.CurTime ||
            !MobQuery.TryComp(args.OtherEntity, out var otherMob) ||
            !MobState.IsAlive(args.OtherEntity, otherMob) ||
            !Random.Prob(ent.Comp.SqueakOnCollideChance))
        {
            return;
        }

        ent.Comp.NextSqueakAt = Timing.CurTime + ent.Comp.SqueakCooldown;
        _audio.PlayPvs(ent.Comp.SqueakSound, ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-rodent-squeaks", ("rodent", ent.Owner)), ent.Owner, args.OtherEntity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCRodentBehaviorComponent>();
        while (query.MoveNext(out var uid, out var rodent))
        {
            if (!MobState.IsAlive(uid))
                continue;

            if (ActorQuery.HasComp(uid))
            {
                WakeRodent((uid, rodent));
                continue;
            }

            if (rodent.Sleeping)
            {
                UpdateSleepingRodent((uid, rodent), now);
                continue;
            }

            if (rodent.NextThinkAt > now)
                continue;

            rodent.NextThinkAt = now + rodent.ThinkCooldown;
            if (!Container.IsEntityInContainer(uid) && Random.Prob(rodent.SleepChance))
                SleepRodent((uid, rodent));
        }
    }

    private void UpdateSleepingRodent(Entity<RMCRodentBehaviorComponent> ent, TimeSpan now)
    {
        if (ent.Comp.SleepUntil <= now ||
            Random.Prob(ent.Comp.WakeChance))
        {
            WakeRodent(ent);
            return;
        }

        if (ent.Comp.NextSnuffleAt > now ||
            !Random.Prob(ent.Comp.SnuffleChance))
        {
            return;
        }

        ent.Comp.NextSnuffleAt = now + ent.Comp.SnuffleCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-rodent-snuffles", ("rodent", ent.Owner)), ent.Owner);
    }

    private void SleepRodent(Entity<RMCRodentBehaviorComponent> ent)
    {
        ent.Comp.Sleeping = true;
        ent.Comp.SleepUntil = Timing.CurTime + RandomTime(ent.Comp.SleepDurationMin, ent.Comp.SleepDurationMax);
        ent.Comp.NextSnuffleAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.SnuffleCooldown);

        StopMovement(ent.Owner);
        RMCNpc.SleepNPC(ent.Owner);
    }

    private void WakeRodent(Entity<RMCRodentBehaviorComponent> ent)
    {
        if (!ent.Comp.Sleeping)
            return;

        ent.Comp.Sleeping = false;
        ent.Comp.NextThinkAt = Timing.CurTime + ent.Comp.ThinkCooldown;
        RMCNpc.WakeNPC(ent.Owner);
    }
}

public sealed class RMCCatHuntingSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCatHunterComponent, MapInitEvent>(OnCatMapInit);
        SubscribeLocalEvent<RMCCatHunterComponent, ComponentShutdown>(OnCatShutdown);
    }

    private void OnCatMapInit(Entity<RMCCatHunterComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        ent.Comp.NextMeowAt = Timing.CurTime + RandomTime(ent.Comp.MeowCooldownMin, ent.Comp.MeowCooldownMax);
    }

    private void OnCatShutdown(Entity<RMCCatHunterComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.MovementTarget = null;
        ent.Comp.PlayCounter = 0;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCCatHunterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hunter, out var xform))
        {
            if (!MobState.IsAlive(uid))
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                continue;
            }

            TryMeow((uid, hunter), now);

            if (ActorQuery.HasComp(uid) || hunter.NextThinkAt > now)
                continue;

            hunter.NextThinkAt = now + hunter.ThinkCooldown;

            var prey = PickPrey((uid, hunter, xform));
            if (prey == null)
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                continue;
            }

            var preyCoords = Transform.GetMoverCoordinates(prey.Value);
            if (!Transform.GetMoverCoordinates(uid).TryDistance(EntityManager, preyCoords, out var distance))
                continue;

            if (hunter.MovementTarget != prey.Value)
            {
                hunter.MovementTarget = prey;
                hunter.PlayCounter = 0;
                Popup.PopupEntity(Loc.GetString("rmc-cat-pounces-at", ("cat", uid), ("prey", prey.Value)), uid);
            }

            TryThreatenPrey(uid, prey.Value, hunter, distance, now);

            if (distance > hunter.AttackRange)
            {
                TryMoveTowards(uid, preyCoords, hunter.MoveSpeed);
                continue;
            }

            if (hunter.PlayCounter >= hunter.MaxPlayAttacks)
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                hunter.NextThinkAt = now + hunter.PlayBreakCooldown;
                continue;
            }

            AttackPrey(uid, prey.Value, hunter);
        }
    }

    private EntityUid? PickPrey(Entity<RMCCatHunterComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var prey in Lookup.GetEntitiesInRange<RMCAnimalPreyComponent>(mapCoords, ent.Comp1.SearchRange))
        {
            if (prey.Owner == ent.Owner || !ValidLivingMob(prey.Owner))
                continue;

            if (ent.Comp1.PreyWhitelist != null && _whitelist.IsWhitelistFail(ent.Comp1.PreyWhitelist, prey.Owner))
                continue;

            var preyCoords = Transform.GetMapCoordinates(prey.Owner);
            var distance = (preyCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = prey.Owner;
            bestDistance = distance;
        }

        return best;
    }

    private void AttackPrey(EntityUid cat, EntityUid prey, RMCCatHunterComponent hunter)
    {
        hunter.PlayCounter++;

        Popup.PopupEntity(Loc.GetString(PickCatAttackPopup(), ("cat", cat), ("prey", prey)), cat);
        _audio.PlayPvs(hunter.HuntHitSound, cat);

        var damage = ActorQuery.HasComp(prey)
            ? hunter.PlayerPreyDamage
            : hunter.NpcPreyDamage;

        Damageable.TryChangeDamage(prey, damage, origin: cat, tool: cat);
        Stun.TryKnockdown(prey, hunter.PlayerPreyKnockdown, true);
        Stun.TrySlowdown(prey, hunter.PlayerPreySlowdown, true, 0.3f, 0.3f);
    }

    private void TryMeow(Entity<RMCCatHunterComponent> ent, TimeSpan now)
    {
        if (ent.Comp.NextMeowAt > now)
            return;

        ent.Comp.NextMeowAt = now + RandomTime(ent.Comp.MeowCooldownMin, ent.Comp.MeowCooldownMax);
        _audio.PlayPvs(ent.Comp.MeowSound, ent.Owner);
    }

    private void TryThreatenPrey(EntityUid cat, EntityUid prey, RMCCatHunterComponent hunter, float distance, TimeSpan now)
    {
        if (distance > hunter.ThreatenRange ||
            hunter.NextThreatenAt > now ||
            !Random.Prob(hunter.ThreatenChance))
        {
            return;
        }

        hunter.NextThreatenAt = now + hunter.ThreatenCooldown;
        Popup.PopupEntity(Loc.GetString(PickCatThreatenPopup(), ("cat", cat), ("prey", prey)), cat);
    }

    private string PickCatAttackPopup()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-bites-prey",
            1 => "rmc-cat-toys-prey",
            _ => "rmc-cat-chomps-prey",
        };
    }

    private string PickCatThreatenPopup()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-hisses-at",
            1 => "rmc-cat-mrowls",
            _ => "rmc-cat-eyes-hungrily",
        };
    }
}

public sealed class RMCParrotSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCParrotComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCParrotComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RMCParrotComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCParrotComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCParrotComponent, ProjectileDamageDealtEvent>(OnProjectileDamageDealt);
    }

    private void OnMapInit(Entity<RMCParrotComponent> ent, ref MapInitEvent args)
    {
        Container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        SetPerched(ent, false);
    }

    private void OnShutdown(Entity<RMCParrotComponent> ent, ref ComponentShutdown args)
    {
        DropHeldItem(ent);
    }

    private void OnMobStateChanged(Entity<RMCParrotComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Dead)
            DropHeldItem(ent);
    }

    private void OnDamageChanged(Entity<RMCParrotComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.DamageDelta == null ||
            args.DamageDelta.Empty)
            return;

        WakeParrot(ent);

        if (ActorQuery.HasComp(ent.Owner))
            return;

        if (args.Tool is { } tool && HasComp<ProjectileComponent>(tool))
            return;

        if (args.Origin is { } origin && origin != ent.Owner && ValidLivingMob(origin))
            StartDefensiveBehavior(ent, origin);
        else
            StartPanic(ent, args.Origin);
    }

    private void OnProjectileDamageDealt(Entity<RMCParrotComponent> ent, ref ProjectileDamageDealtEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.Empty)
            return;

        WakeParrot(ent);

        if (ActorQuery.HasComp(ent.Owner))
            return;

        StartPanic(ent, args.Origin);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCParrotComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var parrot, out var xform))
        {
            if (!MobState.IsAlive(uid))
                continue;

            if (ActorQuery.HasComp(uid))
            {
                ClearParrotBehavior((uid, parrot), false);
                WakeParrot((uid, parrot));
                continue;
            }

            if (UpdateDefensiveBehavior((uid, parrot, xform), now))
                continue;

            if (parrot.NextThinkAt > now)
                continue;

            parrot.NextThinkAt = now + parrot.ThinkCooldown;

            if (parrot.Perched)
            {
                if (Random.Prob(parrot.WakeChance))
                    WakeParrot((uid, parrot));

                continue;
            }

            if (ValidateHeldItem((uid, parrot)))
            {
                UpdateHeldItemReturn((uid, parrot, xform));
                continue;
            }

            if (!Random.Prob(parrot.StealChance))
                continue;

            if (TryStealGroundItem((uid, parrot, xform)))
                continue;

            TryStealHeldItem((uid, parrot, xform));
        }
    }

    private bool UpdateDefensiveBehavior(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        switch (ent.Comp1.Behavior)
        {
            case RMCParrotBehavior.None:
                return false;
            case RMCParrotBehavior.Panic:
                return UpdatePanic(ent, now);
            case RMCParrotBehavior.Flee:
                return UpdateFlee(ent, now);
            case RMCParrotBehavior.Attack:
                return UpdateAttack(ent, now);
            default:
                ClearParrotBehavior((ent.Owner, ent.Comp1));
                return false;
        }
    }

    private bool UpdatePanic(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        if (now >= ent.Comp1.BehaviorUntil)
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        if (ent.Comp1.BehaviorTarget is { } target &&
            ValidLivingMob(target) &&
            XformQuery.TryGetComponent(target, out var targetXform))
        {
            TryMoveAwayFrom(ent.Owner, targetXform.Coordinates, ent.Comp1.PanicFlySpeed);
            return true;
        }

        if (ent.Comp1.NextPanicMoveAt <= now)
        {
            ent.Comp1.NextPanicMoveAt = now + RandomTime(ent.Comp1.PanicMoveCooldownMin, ent.Comp1.PanicMoveCooldownMax);
            TryMoveRandomly(ent.Owner, ent.Comp1.PanicFlySpeed);
        }

        return true;
    }

    private bool UpdateFlee(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        if (now >= ent.Comp1.BehaviorUntil ||
            ent.Comp1.BehaviorTarget is not { } target ||
            !ValidLivingMob(target) ||
            !XformQuery.TryGetComponent(target, out var targetXform))
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        TryMoveAwayFrom(ent.Owner, targetXform.Coordinates, ent.Comp1.FleeSpeed);
        return true;
    }

    private bool UpdateAttack(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        if (now >= ent.Comp1.BehaviorUntil ||
            ent.Comp1.BehaviorTarget is not { } target ||
            !ValidLivingMob(target) ||
            MobState.IsIncapacitated(target) ||
            !XformQuery.TryGetComponent(target, out var targetXform))
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        var targetCoords = targetXform.Coordinates;
        if (!Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, targetCoords, out var distance))
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        if (distance > ent.Comp1.AttackRange)
        {
            TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.AttackFlySpeed);
            return true;
        }

        StopMovement(ent.Owner);
        if (ent.Comp1.NextAttackAt > now)
            return true;

        ent.Comp1.NextAttackAt = now + ent.Comp1.AttackCooldown;
        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                { "Slash", FixedPoint2.New(Random.NextFloat(ent.Comp1.AttackDamageMin, ent.Comp1.AttackDamageMax)) },
            },
        };

        Damageable.TryChangeDamage(target, damage, origin: ent.Owner, tool: ent.Owner);
        Popup.PopupEntity(Loc.GetString(PickParrotAttackPopup(), ("parrot", ent.Owner), ("target", target)), ent.Owner);
        return true;
    }

    private void StartDefensiveBehavior(Entity<RMCParrotComponent> ent, EntityUid attacker)
    {
        WakeParrot(ent);

        if (IsWeakTarget(attacker, ent.Comp.WeakTargetDamageFraction))
        {
            ent.Comp.Behavior = RMCParrotBehavior.Attack;
            ent.Comp.BehaviorTarget = attacker;
            ent.Comp.BehaviorUntil = Timing.CurTime + ent.Comp.AttackDuration;
            ent.Comp.NextAttackAt = Timing.CurTime;
            ent.Comp.NextThinkAt = ent.Comp.BehaviorUntil;
            Popup.PopupEntity(Loc.GetString("rmc-parrot-turns-on", ("parrot", ent.Owner), ("target", attacker)), ent.Owner);
            return;
        }

        DropHeldItem(ent);
        ent.Comp.Behavior = RMCParrotBehavior.Flee;
        ent.Comp.BehaviorTarget = attacker;
        ent.Comp.BehaviorUntil = Timing.CurTime + ent.Comp.FleeDuration;
        ent.Comp.NextThinkAt = ent.Comp.BehaviorUntil;
        Popup.PopupEntity(Loc.GetString("rmc-parrot-flees", ("parrot", ent.Owner), ("target", attacker)), ent.Owner);
    }

    private void StartPanic(Entity<RMCParrotComponent> ent, EntityUid? threat)
    {
        WakeParrot(ent);
        DropHeldItem(ent);

        ent.Comp.Behavior = RMCParrotBehavior.Panic;
        ent.Comp.BehaviorTarget = threat;
        ent.Comp.BehaviorUntil = Timing.CurTime + ent.Comp.ShotPanicDuration;
        ent.Comp.NextPanicMoveAt = TimeSpan.Zero;
        ent.Comp.NextThinkAt = ent.Comp.BehaviorUntil;
        Popup.PopupEntity(Loc.GetString("rmc-parrot-panics", ("parrot", ent.Owner)), ent.Owner);
    }

    private void ClearParrotBehavior(Entity<RMCParrotComponent> ent, bool stopMovement = true)
    {
        if (ent.Comp.Behavior == RMCParrotBehavior.None)
            return;

        ent.Comp.Behavior = RMCParrotBehavior.None;
        ent.Comp.BehaviorTarget = null;
        ent.Comp.BehaviorUntil = TimeSpan.Zero;

        if (stopMovement)
            StopMovement(ent.Owner);
    }

    private bool IsWeakTarget(EntityUid target, float weakDamageFraction)
    {
        const float missingThresholdWeakDamage = 50f;

        if (MobState.IsIncapacitated(target))
            return true;

        if (!DamageableQuery.TryComp(target, out var damageable) ||
            !ThresholdsQuery.TryComp(target, out var thresholds))
        {
            return false;
        }

        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state != Content.Shared.Mobs.MobState.Dead || threshold <= FixedPoint2.Zero)
                continue;

            return damageable.TotalDamage.Float() / threshold.Float() >= weakDamageFraction;
        }

        return damageable.TotalDamage.Float() >= missingThresholdWeakDamage;
    }

    private string PickParrotAttackPopup()
    {
        return Random.Next(2) == 0
            ? "rmc-parrot-pecks"
            : "rmc-parrot-claws";
    }

    private bool ValidateHeldItem(Entity<RMCParrotComponent> ent)
    {
        if (ent.Comp.HeldItem is not { } held || !TerminatingOrDeleted(held))
            return ent.Comp.HeldItem != null;

        ent.Comp.HeldItem = null;
        return false;
    }

    private void UpdateHeldItemReturn(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        if (ent.Comp1.Perch == null || TerminatingOrDeleted(ent.Comp1.Perch.Value))
            ent.Comp1.Perch = PickPerch(ent);

        if (ent.Comp1.Perch is not { } perch || !XformQuery.TryGetComponent(perch, out var perchXform))
        {
            DropHeldItem((ent.Owner, ent.Comp1));
            return;
        }

        var perchCoords = perchXform.Coordinates;
        if (Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, perchCoords, out var distance) &&
            distance <= ent.Comp1.PerchArriveRange)
        {
            DropHeldItem((ent.Owner, ent.Comp1), perchCoords);
            SetPerched((ent.Owner, ent.Comp1), true);
            Popup.PopupEntity(Loc.GetString("rmc-parrot-perches", ("parrot", ent.Owner)), ent.Owner);
            return;
        }

        TryMoveTowards(ent.Owner, perchCoords, ent.Comp1.FlySpeed);
    }

    private bool TryStealGroundItem(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var item in Lookup.GetEntitiesInRange<ItemComponent>(mapCoords, ent.Comp1.SearchRange))
        {
            if (!CanStealItem(ent.Comp1, item))
                continue;

            if (Container.IsEntityOrParentInContainer(item.Owner))
                continue;

            var itemCoords = Transform.GetMapCoordinates(item.Owner);
            var distance = (itemCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = item.Owner;
            bestDistance = distance;
        }

        if (best == null)
            return false;

        var targetCoords = Transform.GetMoverCoordinates(best.Value);
        if (bestDistance > ent.Comp1.PickupRange)
        {
            TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.FlySpeed);
            return true;
        }

        return TryStoreItem((ent.Owner, ent.Comp1), best.Value);
    }

    private bool TryStealHeldItem(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        foreach (var hands in Lookup.GetEntitiesInRange<HandsComponent>(mapCoords, ent.Comp1.SearchRange))
        {
            if (hands.Owner == ent.Owner ||
                !MobState.IsAlive(hands.Owner) ||
                !_hands.TryGetActiveItem((hands.Owner, hands.Comp), out var held) ||
                held == null ||
                !ItemQuery.TryComp(held.Value, out var item) ||
                !CanStealItem(ent.Comp1, (held.Value, item)))
            {
                continue;
            }

            var ownerCoords = Transform.GetMoverCoordinates(hands.Owner);
            if (!Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, ownerCoords, out var distance))
                continue;

            if (distance > ent.Comp1.PickupRange)
            {
                TryMoveTowards(ent.Owner, ownerCoords, ent.Comp1.FlySpeed);
                return true;
            }

            if (!_hands.TryDrop((hands.Owner, hands.Comp), held.Value, Transform.GetMoverCoordinates(ent.Owner), false, false))
                continue;

            if (TryStoreItem((ent.Owner, ent.Comp1), held.Value))
                return true;
        }

        return false;
    }

    private bool TryStoreItem(Entity<RMCParrotComponent> ent, EntityUid item)
    {
        if (!ItemQuery.TryComp(item, out var itemComp) ||
            !CanStealItem(ent.Comp, (item, itemComp)) ||
            !Container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
        {
            return false;
        }

        if (!Container.Insert((item, null, null, null), container, force: true))
            return false;

        ent.Comp.HeldItem = item;
        WakeParrot(ent);
        Popup.PopupEntity(Loc.GetString("rmc-parrot-steals", ("parrot", ent.Owner), ("item", item)), ent.Owner);
        return true;
    }

    private bool CanStealItem(RMCParrotComponent parrot, Entity<ItemComponent> item)
    {
        return item.Owner.Valid &&
               !TerminatingOrDeleted(item.Owner) &&
               parrot.HeldItem == null &&
               parrot.StolenSizes.Contains(item.Comp.Size);
    }

    private EntityUid? PickPerch(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var surface in Lookup.GetEntitiesInRange<PlaceableSurfaceComponent>(mapCoords, ent.Comp1.PerchRange))
        {
            if (surface.Owner == ent.Owner ||
                ent.Comp1.PerchWhitelist != null && _whitelist.IsWhitelistFail(ent.Comp1.PerchWhitelist, surface.Owner) ||
                !XformQuery.TryGetComponent(surface.Owner, out var surfaceXform) ||
                !surfaceXform.Anchored)
            {
                continue;
            }

            var distance = (Transform.GetMapCoordinates((surface.Owner, surfaceXform)).Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = surface.Owner;
            bestDistance = distance;
        }

        return best;
    }

    private void DropHeldItem(Entity<RMCParrotComponent> ent, EntityCoordinates? coordinates = null)
    {
        if (!Container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        var destination = coordinates ?? Transform(ent.Owner).Coordinates;
        Container.EmptyContainer(container, true, destination);
        ent.Comp.HeldItem = null;
    }

    private void SetPerched(Entity<RMCParrotComponent> ent, bool perched)
    {
        ent.Comp.Perched = perched;
        _appearance.SetData(ent.Owner, RMCParrotVisuals.Perched, perched);

        if (perched)
        {
            StopMovement(ent.Owner);
            RMCNpc.SleepNPC(ent.Owner);
        }
        else
        {
            RMCNpc.WakeNPC(ent.Owner);
        }
    }

    private void WakeParrot(Entity<RMCParrotComponent> ent)
    {
        if (!ent.Comp.Perched)
            return;

        SetPerched(ent, false);
    }
}

public sealed class RMCFarmAnimalSystem : RMCAnimalSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGoatTemperComponent, MapInitEvent>(OnGoatMapInit);
        SubscribeLocalEvent<RMCCowTippableComponent, DisarmedEvent>(OnCowDisarmed);
        SubscribeLocalEvent<RMCChickenFedEggLayerComponent, MapInitEvent>(OnChickenMapInit);
        SubscribeLocalEvent<RMCChickenFedEggLayerComponent, InteractUsingEvent>(OnChickenInteractUsing);
        SubscribeLocalEvent<RMCChickenEggHatchComponent, MapInitEvent>(OnChickenEggMapInit);
        SubscribeLocalEvent<RMCChickGrowthComponent, MapInitEvent>(OnChickMapInit);
    }

    private void OnGoatMapInit(Entity<RMCGoatTemperComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
    }

    private void OnCowDisarmed(Entity<RMCCowTippableComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled ||
            args.Target != ent.Owner ||
            !MobState.IsAlive(ent.Owner) ||
            ent.Comp.NextTipAt > Timing.CurTime)
        {
            return;
        }

        ent.Comp.NextTipAt = Timing.CurTime + ent.Comp.TipCooldown;
        ent.Comp.TippedUntil = Timing.CurTime + ent.Comp.TipTime;
        Stun.TryKnockdown(ent.Owner, ent.Comp.TipTime, true);
        Popup.PopupEntity(Loc.GetString("rmc-cow-tipped-user", ("cow", ent.Owner)), ent.Owner, args.Source);
        Popup.PopupEntity(Loc.GetString("rmc-cow-tipped-others", ("cow", ent.Owner), ("user", args.Source)), ent.Owner);
        args.IsStunned = true;
        args.Handled = true;
    }

    private void OnChickenMapInit(Entity<RMCChickenFedEggLayerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextLayCheckAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.LayCheckCooldown);
    }

    private void OnChickenEggMapInit(Entity<RMCChickenEggHatchComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.HatchAt = Timing.CurTime + RandomTime(ent.Comp.HatchMin, ent.Comp.HatchMax);
    }

    private void OnChickMapInit(Entity<RMCChickGrowthComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.GrowAt = Timing.CurTime + RandomTime(ent.Comp.GrowMin, ent.Comp.GrowMax);
    }

    private void OnChickenInteractUsing(Entity<RMCChickenFedEggLayerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !MobState.IsAlive(ent.Owner) ||
            !Tags.HasTag(args.Used, ent.Comp.FeedTag))
        {
            return;
        }

        if (ent.Comp.EggCredits >= ent.Comp.MaxEggCredits)
        {
            Popup.PopupEntity(Loc.GetString("rmc-chicken-not-hungry", ("chicken", ent.Owner)), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        var added = Random.Next(ent.Comp.MinFeedCredits, ent.Comp.MaxFeedCredits + 1);
        ent.Comp.EggCredits = Math.Min(ent.Comp.MaxEggCredits, ent.Comp.EggCredits + added);
        QueueDel(args.Used);
        Popup.PopupEntity(Loc.GetString("rmc-chicken-fed", ("chicken", ent.Owner)), ent.Owner, args.User);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGoats();
        UpdateChickenEggs();
        UpdateChicks();
        UpdateChickens();
    }

    private void UpdateGoats()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCGoatTemperComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var goat, out var xform))
        {
            if (goat.NextThinkAt > now)
                continue;

            goat.NextThinkAt = now + goat.ThinkCooldown;
            if (!MobState.IsAlive(uid))
                continue;

            var hostiles = Faction.GetHostiles(uid).ToArray();
            if (hostiles.Length > 0)
            {
                if (!Random.Prob(goat.CalmChance))
                    continue;

                foreach (var hostile in hostiles)
                    Faction.DeAggroEntity(uid, hostile);

                Popup.PopupEntity(Loc.GetString("rmc-goat-calms", ("goat", uid)), uid);
                continue;
            }

            if (!Random.Prob(goat.MadChance))
                continue;

            var candidates = new List<EntityUid>();
            var mapCoords = Transform.GetMapCoordinates((uid, xform));
            foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, goat.SearchRange))
            {
                if (mob.Owner == uid ||
                    HasComp<RMCGoatTemperComponent>(mob.Owner) ||
                    !MobState.IsAlive(mob.Owner, mob.Comp))
                {
                    continue;
                }

                candidates.Add(mob.Owner);
            }

            if (candidates.Count == 0)
                continue;

            var target = Random.Pick(candidates);
            Faction.AggroEntity(uid, target);
            Popup.PopupEntity(Loc.GetString("rmc-goat-evil-gleam", ("goat", uid)), uid);
        }
    }

    private void UpdateChickenEggs()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCChickenEggHatchComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var egg, out var xform))
        {
            if (egg.HatchAt > now)
                continue;

            if (Container.IsEntityInContainer(uid))
            {
                egg.HatchAt = now + RandomTime(egg.HatchMin, egg.HatchMax);
                continue;
            }

            Spawn(egg.SpawnPrototype, xform.Coordinates);
            Popup.PopupEntity(Loc.GetString("rmc-chicken-egg-hatches", ("egg", uid)), uid);
            QueueDel(uid);
        }
    }

    private void UpdateChicks()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCChickGrowthComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var chick, out var xform))
        {
            if (chick.GrowAt > now || !MobState.IsAlive(uid))
                continue;

            Spawn(Random.Pick(chick.MaturePrototypes), xform.Coordinates);
            QueueDel(uid);
        }
    }

    private void UpdateChickens()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCChickenFedEggLayerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var chicken, out var xform))
        {
            if (chicken.NextLayCheckAt > now)
                continue;

            chicken.NextLayCheckAt = now + chicken.LayCheckCooldown;

            if (!MobState.IsAlive(uid) ||
                chicken.EggCredits <= 0 ||
                !Random.Prob(chicken.LayChance))
            {
                continue;
            }

            var egg = chicken.EggPrototype;
            var mapCoords = Transform.GetMapCoordinates((uid, xform));
            if (Random.Prob(chicken.FertilizedEggChance) &&
                CountNearby<RMCChickenComponent>(mapCoords, chicken.ChickenCapRange) < chicken.MaxNearbyChickens)
            {
                egg = chicken.FertilizedEggPrototype;
            }

            Spawn(egg, xform.Coordinates);
            chicken.EggCredits--;
            Popup.PopupEntity(Loc.GetString("rmc-chicken-lays-egg", ("chicken", uid)), uid);
        }
    }
}

public sealed class RMCAnimalSpawnerSystem : RMCAnimalSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCAnimalSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCAnimalSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (Random.Prob(ent.Comp.InitialChance))
            TrySpawnAnimal(ent);

        ent.Comp.NextLateSpawnAt = Timing.CurTime + RandomTime(ent.Comp.LateSpawnMin, ent.Comp.LateSpawnMax);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCAnimalSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (spawner.NextLateSpawnAt > now)
                continue;

            if (CountAliveSpawned(spawner) >= spawner.MaxAlive || HasWitness(uid, spawner.WitnessRange))
            {
                spawner.NextLateSpawnAt = now + RandomTime(spawner.RetryMin, spawner.RetryMax);
                continue;
            }

            TrySpawnAnimal((uid, spawner));
            spawner.NextLateSpawnAt = now + RandomTime(spawner.RetryMin, spawner.RetryMax);
        }
    }

    private bool TrySpawnAnimal(Entity<RMCAnimalSpawnerComponent> ent)
    {
        if (CountAliveSpawned(ent.Comp) >= ent.Comp.MaxAlive)
            return false;

        if (!XformQuery.TryGetComponent(ent.Owner, out var xform))
            return false;

        var spawned = Spawn(ent.Comp.Prototype, xform.Coordinates);
        ent.Comp.Spawned.Add(spawned);
        return true;
    }

    private int CountAliveSpawned(RMCAnimalSpawnerComponent spawner)
    {
        var count = 0;
        for (var i = spawner.Spawned.Count - 1; i >= 0; i--)
        {
            var spawned = spawner.Spawned[i];
            if (TerminatingOrDeleted(spawned))
            {
                spawner.Spawned.RemoveAt(i);
                continue;
            }

            if (ValidLivingMob(spawned))
                count++;
        }

        return count;
    }

    private bool HasWitness(EntityUid spawner, float range)
    {
        if (!XformQuery.TryGetComponent(spawner, out var spawnerXform))
            return false;

        var spawnerCoords = Transform.GetMapCoordinates((spawner, spawnerXform));
        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var mob, out var xform))
        {
            if (!MobState.IsAlive(uid, mob))
                continue;

            var coords = Transform.GetMapCoordinates((uid, xform));
            if (coords.MapId == spawnerCoords.MapId &&
                (coords.Position - spawnerCoords.Position).Length() <= range)
            {
                return true;
            }
        }

        return false;
    }
}
