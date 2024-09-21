using Content.Server.GameTicking;
using Content.Server.Movement.Components;
using Content.Server.Movement.Systems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Weapons.Ranged.Prediction;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Weapons.Ranged.Prediction;

public sealed class GunPredictionSystem : SharedGunPredictionSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly LagCompensationSystem _lagCompensation = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private readonly Dictionary<(Guid, int), EntityUid> _predicted = new();
    private readonly List<(PredictedProjectileHitEvent Event, ICommonSession Player)> _predictedHits = new();
    private bool _gunPredictionPreventCollision;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<LagCompensationComponent> _lagCompensationQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<PredictedProjectileServerComponent> _predictedProjectileServerQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _lagCompensationQuery = GetEntityQuery<LagCompensationComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _predictedProjectileServerQuery = GetEntityQuery<PredictedProjectileServerComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RequestShootEvent>(OnShootRequest);
        SubscribeNetworkEvent<PredictedProjectileHitEvent>(OnPredictedProjectileHit);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnSendLinearVelocityAll);

        SubscribeLocalEvent<PredictedProjectileServerComponent, MapInitEvent>(OnPredictedMapInit);
        SubscribeLocalEvent<PredictedProjectileServerComponent, ComponentRemove>(OnPredictedRemove);
        SubscribeLocalEvent<PredictedProjectileServerComponent, EntityTerminatingEvent>(OnPredictedRemove);
        SubscribeLocalEvent<PredictedProjectileServerComponent, PreventCollideEvent>(OnPredictedPreventCollide);

        Subs.CVar(_config, CVars.MaxLinVelocity, OnSendLinearVelocityAll);
        Subs.CVar(_config, RMCCVars.RMCGunPredictionPreventCollision, v => _gunPredictionPreventCollision = v, true);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _predicted.Clear();
        OnSendLinearVelocityAll(ev);
    }

    private void OnShootRequest(RequestShootEvent ev, EntitySessionEventArgs args)
    {
        _gun.ShootRequested(ev.Gun, ev.Coordinates, ev.Target, ev.Shot, args.SenderSession);
    }

    private void OnPredictedMapInit(Entity<PredictedProjectileServerComponent> ent, ref MapInitEvent args)
    {
        _predicted[(ent.Comp.Shooter.UserId, ent.Comp.ClientId)] = ent;
    }

    private void OnPredictedRemove<T>(Entity<PredictedProjectileServerComponent> ent, ref T args)
    {
        _predicted.Remove((ent.Comp.Shooter.UserId, ent.Comp.ClientId));
    }

    private void OnPredictedProjectileHit(PredictedProjectileHitEvent ev, EntitySessionEventArgs args)
    {
        _predictedHits.Add((ev, args.SenderSession));
    }

    private void OnSendLinearVelocityAll<T>(T ev)
    {
        if (_net.IsClient)
            return;

        // TODO gun prediction remove this when we pull engine with a replicated physics maxlinvelocity
        var msg = new MaxLinearVelocityMsg(_config.GetCVar(CVars.MaxLinVelocity));
        RaiseNetworkEvent(msg);
    }

    private void OnPredictedPreventCollide(Entity<PredictedProjectileServerComponent> ent, ref PreventCollideEvent args)
    {
        if (!_gunPredictionPreventCollision)
            return;

        if (args.Cancelled)
            return;

        var other = args.OtherEntity;
        if (!_lagCompensationQuery.TryComp(other, out var otherLagComp) ||
            !_fixturesQuery.TryComp(other, out var otherFixtures) ||
            !_transformQuery.TryComp(other, out var otherTransform))
        {
            return;
        }

        if (!_physicsQuery.TryComp(ent, out var entPhysics))
            return;

        if (!Collides(
                (ent, ent, entPhysics),
                (other, otherLagComp, otherFixtures, args.OtherBody, otherTransform),
                null))
        {
            args.Cancelled = true;
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected && e.NewStatus != SessionStatus.InGame)
            return;

        // TODO gun prediction remove this when we pull engine with a replicated physics maxlinvelocity
        var msg = new MaxLinearVelocityMsg(_config.GetCVar(CVars.MaxLinVelocity));
        RaiseNetworkEvent(msg, e.Session.Channel);
    }

    private bool Collides(
        Entity<PredictedProjectileServerComponent, PhysicsComponent> projectile,
        Entity<LagCompensationComponent, FixturesComponent, PhysicsComponent, TransformComponent> other,
        MapCoordinates? clientCoordinates)
    {
        var projectileCoordinates = _transform.GetMapCoordinates(projectile);
        var projectilePosition = projectileCoordinates.Position;
        // var ping = projectile.Comp1.Shooter.Ping;
        // ping = (short) (75 * 1.5); // TODO
        // var projectileOffset = _physics.GetLinearVelocity(projectile, _transform.ToMapCoordinates(projectileCoordinates).Position) * ping / 1000f;
        // projectileCoordinates = projectileCoordinates.Offset(projectileOffset);

        MapCoordinates lowestCoordinate = default;
        var otherCoordinates = EntityCoordinates.Invalid;
        var ping = projectile.Comp1.Shooter.Channel.Ping;
        // Use 1.5 due to the trip buffer.
        var sentTime = _timing.CurTime - TimeSpan.FromMilliseconds(ping * 1.5);
        var pingTime = TimeSpan.FromMilliseconds(ping);

        foreach (var pos in other.Comp1.Positions)
        {
            otherCoordinates = pos.Item2;
            if (pos.Item1 >= sentTime)
                break;
            else if (lowestCoordinate == default && pos.Item1 >= sentTime - pingTime)
            {
                lowestCoordinate = _transform.ToMapCoordinates(pos.Item2);
            }
        }

        var otherMapCoordinates = otherCoordinates == default
            ? _transform.GetMapCoordinates(other)
            : _transform.ToMapCoordinates(otherCoordinates);

        if (clientCoordinates != null &&
            (clientCoordinates.Value.InRange(otherMapCoordinates, 0.75f) ||
             clientCoordinates.Value.InRange(lowestCoordinate, 0.5f)))
        {
            otherMapCoordinates = clientCoordinates.Value;
        }

        var transform = new Transform(otherMapCoordinates.Position, 0);
        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in other.Comp2.Fixtures.Values)
        {
            if ((fixture.CollisionLayer & projectile.Comp2.CollisionMask) == 0)
                continue;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                bounds = bounds.Union(boundy);
            }
        }

        bounds = bounds.Enlarged(0.3f);
        if (bounds.Contains(projectilePosition))
            return true;

        var projectileVelocity = _physics.GetLinearVelocity(projectile, projectile.Comp2.LocalCenter);
        projectilePosition = projectileCoordinates.Position + projectileVelocity / _timing.TickRate / 1.5f;
        if (bounds.Contains(projectilePosition))
            return true;

        return false;
    }

    private void ProcessPredictedHit(PredictedProjectileHitEvent ev, ICommonSession player)
    {
        if (!_predicted.TryGetValue((player.UserId, ev.Projectile), out var projectile))
            return;

        if (!_predictedProjectileServerQuery.TryComp(projectile, out var predictedProjectile) ||
            predictedProjectile.Hit)
        {
            return;
        }

        if (predictedProjectile.Shooter.UserId != player.UserId.UserId)
            return;

        if (!_projectileQuery.TryComp(projectile, out var projectileComp) ||
            !_physicsQuery.TryComp(projectile, out var projectilePhysics))
        {
            return;
        }

        predictedProjectile.Hit = true;
        foreach (var (netEnt, clientPos) in ev.Hit)
        {
            if (GetEntity(netEnt) is not { Valid: true } hit)
                continue;

            if (!_lagCompensationQuery.TryComp(hit, out var otherLagComp) ||
                !_fixturesQuery.TryComp(hit, out var otherFixtures) ||
                !_physicsQuery.TryComp(hit, out var otherPhysics) ||
                !_transformQuery.TryComp(hit, out var otherTransform))
            {
                continue;
            }

            if (!Collides(
                    (projectile, predictedProjectile, projectilePhysics),
                    (hit, otherLagComp, otherFixtures, otherPhysics, otherTransform),
                    clientPos))
            {
                Log.Info("not hit");
                continue;
            }

            Log.Info("hit");

            _projectile.ProjectileCollide((projectile, projectileComp, projectilePhysics), hit, true);
        }
    }

    public override void Update(float frameTime)
    {
        try
        {
            foreach (var ev in _predictedHits)
            {
                ProcessPredictedHit(ev.Event, ev.Player);
            }
        }
        finally
        {
            _predictedHits.Clear();
        }

        var predicted = EntityQueryEnumerator<PredictedProjectileHitComponent, TransformComponent>();
        while (predicted.MoveNext(out var uid, out var hit, out var xform))
        {
            var origin = hit.Origin;
            var coordinates = xform.Coordinates;
            if (!origin.TryDistance(EntityManager, _transform, coordinates, out var distance) ||
                distance >= hit.Distance)
            {
                QueueDel(uid);
            }
        }
    }
}
