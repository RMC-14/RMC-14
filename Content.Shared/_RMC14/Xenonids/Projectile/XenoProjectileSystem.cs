using System.Numerics;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Weapons.Ranged.Prediction;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Projectile;

public sealed class XenoProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedGunPredictionSystem _gunPrediction = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCLagCompensationSystem _rmcLagCompensation = default!;
    [Dependency] private readonly CMPoweredLightSystem _rmcPoweredLight = default!;
    [Dependency] private readonly RMCPseudoRandomSystem _rmcPseudoRandom = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<PreventAttackLightOffComponent> _preventAttackLightOffQuery;

    private int _limitHitsId;

    public override void Initialize()
    {
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _preventAttackLightOffQuery = GetEntityQuery<PreventAttackLightOffComponent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<XenoProjectilePredictedHitEvent>(OnPredictedHit);

        SubscribeLocalEvent<XenoProjectileShooterComponent, ComponentRemove>(OnShooterRemove);
        SubscribeLocalEvent<XenoProjectileShooterComponent, EntityTerminatingEvent>(OnShooterRemove);

        SubscribeLocalEvent<XenoProjectileShotComponent, ComponentRemove>(OnShotRemove);
        SubscribeLocalEvent<XenoProjectileShotComponent, EntityTerminatingEvent>(OnShotRemove);

        SubscribeLocalEvent<XenoClientProjectileShotComponent, StartCollideEvent>(OnShotCollide);

        SubscribeLocalEvent<XenoProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<XenoProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<XenoProjectileComponent, CMClusterSpawnedEvent>(OnClusterSpawned);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _limitHitsId = 0;
    }

    private void OnPredictedHit(XenoProjectilePredictedHitEvent msg, EntitySessionEventArgs args)
    {
        if (_net.IsClient || !_gunPrediction.GunPrediction)
            return;

        if (args.SenderSession.AttachedEntity is not { } ent)
            return;

        if (GetEntity(msg.Target) is not { Valid: true } target)
            return;

        if (!TryComp(ent, out XenoProjectileShooterComponent? shooter) ||
            shooter.Shot.Count == 0)
        {
            return;
        }

        if (!shooter.Shot.TryFirstOrNull(e => CompOrNull<XenoProjectileShotComponent>(e)?.Id == msg.Id, out var shot))
            return;

        if (TerminatingOrDeleted(shot))
            return;

        _rmcLagCompensation.SetLastRealTick(args.SenderSession.UserId, msg.LastRealTick);
        var coordinates = _transform.ToMapCoordinates(_rmcLagCompensation.GetCoordinates(target, args.SenderSession));

        if (!TryComp(shot, out ProjectileComponent? projectile) ||
            !TryComp(shot, out PhysicsComponent? physics))
        {
            return;
        }

        if (!_rmcLagCompensation.Collides(target, (shot.Value, physics), coordinates))
            return;

        _projectile.ProjectileCollide((shot.Value, projectile, physics), target, true);
    }

    private void OnShooterRemove<T>(Entity<XenoProjectileShooterComponent> ent, ref T args)
    {
        if (_timing.ApplyingState)
            return;

        foreach (var shot in ent.Comp.Shot)
        {
            RemCompDeferred<XenoProjectileShotComponent>(shot);
        }

        ent.Comp.Shot.Clear();
        Dirty(ent);
    }

    private void OnShotRemove<T>(Entity<XenoProjectileShotComponent> ent, ref T args)
    {
        if (ent.Comp.ShooterEnt is not { } shooter)
            return;

        if (TryComp(shooter, out XenoProjectileShooterComponent? shooterComp) &&
            shooterComp.Shot.Remove(ent))
        {
            Dirty(shooter, shooterComp);
        }
    }

    private void OnShotCollide(Entity<XenoClientProjectileShotComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsServer || !IsClientSide(ent))
            return;

        if (!TryComp(ent, out XenoProjectileShotComponent? shot))
            return;

        var ev = new XenoProjectilePredictedHitEvent(
            shot.Id,
            GetNetEntity(args.OtherEntity),
            _rmcLagCompensation.GetLastRealTick(null)
        );
        RaiseNetworkEvent(ev);
    }

    private void OnPreventCollide(Entity<XenoProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (_preventAttackLightOffQuery.HasComp(args.OtherEntity) &&
            _rmcPoweredLight.IsOff(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.DeleteOnFriendlyXeno)
            return;

        if (_hive.FromSameHive(ent.Owner, args.OtherEntity) &&
            (HasComp<XenoComponent>(args.OtherEntity) || HasComp<HiveCoreComponent>(args.OtherEntity)))
            args.Cancelled = true;
    }

    private void OnProjectileHit(Entity<XenoProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (_hive.FromSameHive(ent.Owner, args.Target))
        {
            args.Handled = true;

            if (_net.IsServer || IsClientSide(ent))
                QueueDel(ent);

            return;
        }

        if (HasComp<XenoComponent>(args.Target))
            args.Damage = _xeno.TryApplyXenoProjectileDamageMultiplier(args.Target, args.Damage);

        if (_projectileQuery.TryComp(ent, out var projectile) &&
            projectile.Shooter is { } shooter)
        {
            var ev = new XenoProjectileHitUserEvent(args.Target);
            RaiseLocalEvent(shooter, ref ev);
        }
    }

    private void OnClusterSpawned(Entity<XenoProjectileComponent> ent, ref CMClusterSpawnedEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        foreach (var spawned in args.Spawned)
        {
            _hive.SetHive(spawned, hive);
        }
    }

    public bool TryShoot(
        EntityUid xeno,
        EntityCoordinates targetCoords,
        FixedPoint2 plasma,
        EntProtoId projectileId,
        SoundSpecifier? sound,
        int shots,
        Angle deviation,
        float speed,
        float? stopAtDistance = null,
        EntityUid? target = null,
        bool predicted = true,
        int? projectileHitLimit = null)
    {
        if (!predicted && _net.IsClient)
            return false;

        var origin = _transform.GetMapCoordinates(xeno);
        var targetMap = _transform.ToMapCoordinates(targetCoords);
        if (origin.MapId != targetMap.MapId ||
            origin.Position == targetMap.Position)
        {
            return false;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno, plasma))
            return false;

        _audio.PlayPredicted(sound, xeno, xeno);
        if (_net.IsClient && !_gunPrediction.GunPrediction || !_timing.IsFirstTimePredicted)
            return true;

        var ammoShotEvent = new AmmoShotEvent { FiredProjectiles = new List<EntityUid>(shots) };

        if (target != null && HasComp<MobStateComponent>(target) && !_xeno.CanAbilityAttackTarget(xeno, target.Value))
            target = null;

        XenoProjectileShooterComponent? shooter = null;
        var shooterPlayer = CompOrNull<ActorComponent>(xeno)?.PlayerSession;
        var xoroshiro = _rmcPseudoRandom.GetXoroshiro64S(xeno);

        var originalDiff = targetMap.Position - origin.Position;
        var halfDeviation = deviation / 2;
        if (projectileHitLimit != null)
            _limitHitsId++;

        for (var i = 0; i < shots; i++)
        {
            // center projectile has no deviation; others are randomly offset within deviation
            var angleOffset = Angle.Zero;
            if (i > 0 && deviation != Angle.Zero)
                angleOffset = _rmcPseudoRandom.NextAngle(ref xoroshiro, -halfDeviation, halfDeviation);

            var projTarget = new MapCoordinates(origin.Position + angleOffset.RotateVec(originalDiff), targetMap.MapId);

            var diff = projTarget.Position - origin.Position;
            var projectile = Spawn(projectileId, origin);
            diff *= speed / diff.Length();

            _gun.ShootProjectile(projectile, diff, Vector2.Zero, xeno, xeno, speed);

            ammoShotEvent.FiredProjectiles.Add(projectile);

            // let hive member logic apply
            EnsureComp<XenoProjectileComponent>(projectile);

            _hive.SetSameHive(xeno, projectile);

            if (stopAtDistance != null)
            {
                var fixedDistanceComp = EnsureComp<ProjectileFixedDistanceComponent>(projectile);
                fixedDistanceComp.FlyEndTime = _timing.CurTime + TimeSpan.FromSeconds(stopAtDistance.Value / speed);
                Dirty(projectile, fixedDistanceComp);
            }

            if (target != null)
            {
                var targeted = EnsureComp<TargetedProjectileComponent>(projectile);
                targeted.Target = target.Value;
                Dirty(projectile, targeted);
            }

            if (projectileHitLimit != null)
            {
                var limitHits = EnsureComp<ProjectileLimitHitsComponent>(projectile);
                limitHits.Limit = projectileHitLimit.Value;
                limitHits.OriginEntity = xeno;
                limitHits.ExtraId = _limitHitsId;
                Dirty(projectile, limitHits);
            }

            if (predicted)
            {
                shooter ??= EnsureComp<XenoProjectileShooterComponent>(xeno);
                shooter.Shot.Add(projectile);
                Dirty(xeno, shooter);

                var shot = EnsureComp<XenoProjectileShotComponent>(projectile);
                shot.Id = shooter.NextId++;
                shot.Shooter = shooterPlayer;
                shot.ShooterEnt = xeno;
                Dirty(projectile, shot);
            }

            if (_net.IsServer)
                continue;

            EnsureComp<XenoClientProjectileShotComponent>(projectile);
            _physics.UpdateIsPredicted(projectile);
        }

        RaiseLocalEvent(xeno, ammoShotEvent);
        return true;
    }
}
