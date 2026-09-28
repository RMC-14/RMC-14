using System.Numerics;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Projectiles;
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
using Robust.Shared.Serialization.Manager.Exceptions;
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
    [Dependency] private readonly SharedRMCLagCompensationSystem _rmcLag = default!;
    [Dependency] private readonly CMPoweredLightSystem _rmcPoweredLight = default!;
    [Dependency] private readonly RMCPseudoRandomSystem _rmcPseudoRandom = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<PreventAttackLightOffComponent> _preventAttackLightOffQuery;

    private int _limitHitsId;
    private bool _logPrediction = false;
    private bool _predictingSpecificShooter = false;
    private List<(EntityUid Shooter, GameTick PredictedHitTick, XenoProjectilePredictedHitEvent Message, EntitySessionEventArgs Args)> _predictedHitMessages = [];

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

        SubscribeLocalEvent<XenoClientProjectileShotComponent, ProjectileHitEvent>(OnShotHit);

        SubscribeLocalEvent<XenoProjectileComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<XenoProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<XenoProjectileComponent, CMClusterSpawnedEvent>(OnClusterSpawned);

        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _limitHitsId = 0;
        _predictedHitMessages.Clear();
    }

    private void OnPredictedHit(XenoProjectilePredictedHitEvent msg, EntitySessionEventArgs args)
    {
        if (_net.IsClient || !_gunPrediction.GunPrediction)
            return;

        if (msg.Tick > _timing.CurTick + 10)
        {
            // protection against naughty clients, or weird networking issues, or server is lagging bad.
            Log.Warning($"Discarding extremely early predicted hit message from '{args.SenderSession} for tick {msg.Tick}. Current tick is {_timing.CurTick}.");
            return;
        }

        if (args.SenderSession.AttachedEntity is not { } ent)
            return;

        if (_logPrediction)
            Log.Debug($"""
                Received predicted hit:
                  Session:  {args.SenderSession}
                  Cur Tick: {_timing.CurTick}
                  Target:   {msg.Target}
                  Shot ID:  {msg.Id}
                  Shot At:  {msg.ShotAtTick}
                  Hit Tick: {msg.Tick}
                  Substep:  {msg.Substep}
                """);

        _predictedHitMessages.Add((ent, msg.Tick, msg, args));
    }

    /// <summary>
    /// Handles a predicted hit message. Returns true if the message has been handled,
    /// or false if the message should be handled again later.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    private bool HandlePredictedHit(XenoProjectilePredictedHitEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent
            || GetEntity(msg.Target) is not { Valid: true } target
            || !TryComp<XenoProjectileShooterComponent>(ent, out var shooter))
        {
            if (_logPrediction)
                Log.Warning($"Predicted hit from '{args.SenderSession}' discarded due to invalid data.");
            return true;
        }

        var tick = msg.Tick;
        var substep = msg.Substep;
        var shotTick = msg.ShotAtTick;

        if (substep < 0 || substep > _rmcLag.GetSubsteps())
        {
            Log.Warning($"Predicted hit from '{args.SenderSession}' contained out-of-range substep {substep}. Sanitizing it.");
            substep = Math.Clamp(substep, 0, _rmcLag.GetSubsteps());
        }

        if (tick > _timing.CurTick)
        {
            // This shouldn't happen, ProcessPredictedMessages should be delaying it.
            DebugTools.Assert(!(tick > _timing.CurTick));
            return false;
        }
        else if (tick <= _timing.CurTick - 2) // we allow processing a message up to one tick late
        {
            // This shouldn't happen, ProcessPredictedMessages should have discarded it.
            DebugTools.Assert(!(tick <= _timing.CurTick - 2));
            return true;
        }

        if (shooter.NextId <= msg.Id)
        {
            // The shooter hasn't shot our predicted shot yet.
            // The message is either being processed too early, OR
            // the server is shooting the shot later than expected, OR
            // the server was unable to shoot the projectile as predicted.
            if (_logPrediction)
                Log.Debug($"Predicted hit from '{args.SenderSession}' for shot {msg.Id} at tick {tick}, but the latest shot was {shooter.NextId - 1} at tick {_timing.CurTick})");
            return false;
        }

        if (shooter.Shot.Count == 0
            || !shooter.Shot.TryFirstOrNull(e => CompOrNull<XenoProjectileShotComponent>(e)?.Id == msg.Id, out var shot)
            || TerminatingOrDeleted(shot))
        {
            // The shooter shot our predicted shot, but we failed to find it. It has either hit something
            // or its duration has expired.
            if (_logPrediction)
                Log.Debug($"Predicted hit from '{args.SenderSession}' could not find shot {msg.Id} after it was shot.");
            return true;
        }

        if (!TryComp(shot, out XenoProjectileShotComponent? xenoShot)
            || !TryComp(shot, out ProjectileComponent? projectile)
            || !TryComp(shot, out PhysicsComponent? physics))
        {
            Log.Warning($"Predicted hit from '{args.SenderSession}' found a shot without a necessary component.");
            return true;
        }

        if (projectile.ProjectileSpent)
        {
            if (_logPrediction)
                Log.Debug($"Predicted hit from '{args.SenderSession}' shot {msg.Id} is spent and cannot hit anything anymore.");
            return true;
        }

        // server shot later than predicted, adjust the shot forward to try and hit
        if (xenoShot.ShotAtTick > msg.ShotAtTick)
        {
            if (_logPrediction)
                Log.Debug($"Predicted hit from '{args.SenderSession}' predicted shot at {msg.ShotAtTick} but it was shot at {xenoShot.ShotAtTick}. Adjusting forward.");
            substep += _rmcLag.GetSubsteps();
        }

        // predicted hit message processed late, adjust the shot backwards to try and hit
        if (_timing.CurTick > msg.Tick)
        {
            if (_logPrediction)
                Log.Debug($"Predicted hit from '{args.SenderSession}' on tick {msg.Tick} but it's currently {_timing.CurTick}. Adjusting backwards.");
            substep -= _rmcLag.GetSubsteps();
        }

        if (substep > _rmcLag.GetSubsteps())
        {
            // We're trying to predict over a tick ahead. Delay the message if we can,
            // but it will be discarded if it's too late.
            if (_logPrediction)
                Log.Debug($"Predicted hit from '{args.SenderSession} needs to be pushed forward by {substep} substeps, " +
                    $"above the limit of {_rmcLag.GetSubsteps()}. Delaying processing.");

            return false;
        }

        if (_logPrediction)
            Log.Debug($"""
                Predicted hit checks passed, will test collision. Details:
                  Session Name:   {args.SenderSession}
                  Last Real Tick: {msg.LastRealTick}
                  Shot ID:        {msg.Id}
                  During shoot?   {_predictingSpecificShooter}

                  Cur Tick:       {_timing.CurTick}
                  Pred Hit Tick:  {msg.Tick}

                  Shot Tick:      {xenoShot.ShotAtTick}
                  Pred Shot Tick: {msg.ShotAtTick}

                  Pred Substep:   {msg.Substep}
                  Adjust Substep: {substep}
                """);

        _rmcLag.SetLastRealTick(args.SenderSession.UserId, msg.LastRealTick);
        var hitConfirmed = _rmcLag.Collides(target, (shot.Value, physics), args.SenderSession, substep);

        if (hitConfirmed)
        {
            if (_logPrediction)
                Log.Debug($"Predicted hit from '{args.SenderSession}' ++ CONFIRMED!! ++");

            _projectile.ProjectileCollide((shot.Value, projectile, physics), target, true);
        }
        else if (_logPrediction)
        {
            Log.Warning($"Predicted hit from '{args.SenderSession}' -- denied --");
        }

        return true;
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

    // TODO RMC14 There is a bug with clients trying to predict StartCollideEvent on our version of RT.
    // This should be fixed in the newest versions of RT (2026 or later), and then we can change this back
    // to react to StartCollideEvent.
    private void OnShotHit(Entity<XenoClientProjectileShotComponent> ent, ref ProjectileHitEvent args)
    {
        if (_net.IsServer || !IsClientSide(ent))
            return;

        if (!TryComp(ent, out XenoProjectileShotComponent? shot))
            return;

        // If a collision happens during a re-predicted frame, the projectile is actually at substep 0
        // for the next frame. This is because on tick x, after the physics system runs, objects will
        // actually be at their starting positions for tick x + 1. This includes our projectile.
        // We don't have an up-to-date value for LatestPredictedTick because tick x + 1 hasn't run yet.
        var tick = ent.Comp.LatestPredictedTick;
        var substep = _rmcLag.GetClientSubstep();
        if (!_timing.IsFirstTimePredicted)
        {
            tick += 1;
            substep = 0;
        }

        if (_logPrediction)
        {
            TryComp(args.Target, out TransformComponent? targetTransform);
            TryComp(ent, out TransformComponent? shotTransform);
            Log.Debug($"""
                SENDING PREDICTED PROJECTILE HIT!!
                  Shot ID:        {shot.Id}
                  Cur Tick:       {_timing.CurTick}
                  LastRealTick:   {_rmcLag.GetLastRealTick(null)}
                  Phys Substep:   {_rmcLag.GetCurrentSubstep()}
                  In simulation?  {_timing.InSimulation}
                  ApplyingState?  {_timing.ApplyingState}
                  FirstTimePred?  {_timing.IsFirstTimePredicted}
                  Shot At Tick:   {shot.ShotAtTick}
                  Pred Hit Tick:  {tick}
                  Substep:        {substep}
                  Shot Coords:    {shotTransform?.Coordinates}
                  Target Coords:  {targetTransform?.Coordinates}
                """);
        }

        var ev = new XenoProjectilePredictedHitEvent(
            shot.Id,
            GetNetEntity(args.Target),
            _rmcLag.GetLastRealTick(null),
            tick,
            substep,
            shot.ShotAtTick
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

    /// <summary>
    /// Processes predicted hits that arrived too early.
    /// </summary>
    /// <param name="forShooter">Only process hits for a specific shooter</param>
    private void ProcessPredictedMessages(EntityUid? forShooter = null)
    {
        if (_net.IsClient)
            return;

        for (var i = _predictedHitMessages.Count - 1; i >= 0; --i)
        {
            var item = _predictedHitMessages[i];
            var lastIndex = _predictedHitMessages.Count - 1;
            if (item.PredictedHitTick <= _timing.CurTick - 2) // messages two ticks in the past are considered expired
            {
                if (_logPrediction)
                    Log.Warning("Removed expired prediction message: " +
                        $"Shooter {item.Args.SenderSession}, Shot ID {item.Message.Id}, Tick {item.PredictedHitTick}, CurTick {_timing.CurTick}");
                if (i != lastIndex)
                    _predictedHitMessages[i] = _predictedHitMessages[lastIndex];
                _predictedHitMessages.RemoveAt(lastIndex);
            }
            else if (item.PredictedHitTick <= _timing.CurTick) // process messages on their tick or up to one tick behind
            {
                if (forShooter != null && item.Shooter != forShooter)
                    continue;

                if (!HandlePredictedHit(item.Message, item.Args))
                    continue; // wasn't handled yet, keep it around
                if (i != lastIndex)
                    _predictedHitMessages[i] = _predictedHitMessages[lastIndex];
                _predictedHitMessages.RemoveAt(lastIndex);
            }
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

            var ev = new ProjectileShotEvent(xeno, predicted);
            RaiseLocalEvent(projectile, ref ev);

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
                limitHits.OriginEntityId = xeno.Id;
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
                shot.ShotAtTick = _timing.CurTick;
                Dirty(projectile, shot);
            }

            if (_net.IsServer)
                continue;

            var clientShot = EnsureComp<XenoClientProjectileShotComponent>(projectile);
            clientShot.LatestPredictedTick = _timing.CurTick;
            _physics.UpdateIsPredicted(projectile);
        }

        RaiseLocalEvent(xeno, ammoShotEvent);

        // Client may have already predicted hits for this projectile, check before we test collisions.
        if (_net.IsServer && predicted)
        {
            _predictingSpecificShooter = true;
            ProcessPredictedMessages(xeno);
            _predictingSpecificShooter = false;
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            var shotQuery = EntityQueryEnumerator<XenoClientProjectileShotComponent>();
            while (shotQuery.MoveNext(out var uid, out var comp))
            {
                comp.LatestPredictedTick = _timing.CurTick;
            }
        }
        else // server
        {
            ProcessPredictedMessages();
        }
    }
}
