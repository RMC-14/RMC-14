using Content.Shared._RMC14.CCVar;
using Content.Shared.Coordinates;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Movement;

public abstract class SharedRMCLagCompensationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public float MarginTiles { get; private set; }

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;
    private int _substeps;
    private float _substepTime;
    private bool _logPrediction = false;

    private readonly Dictionary<NetUserId, GameTick> _lastRealTicks = new();


    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        SubscribeNetworkEvent<RMCSetLastRealTickEvent>(OnSetLastRealTick);

        Subs.CVar(_config, RMCCVars.RMCLagCompensationMarginTiles, v => MarginTiles = v, true);
        Subs.CVar(_config, CVars.NetTickrate, UpdateSubsteps, true);
        Subs.CVar(_config, CVars.TargetMinimumTickrate, UpdateSubsteps, true);
    }

    private void OnSetLastRealTick(RMCSetLastRealTickEvent msg, EntitySessionEventArgs args)
    {
        SetLastRealTick(args.SenderSession.UserId, msg.Tick - 1);
    }

    private void UpdateSubsteps(int _)
    {
        // This is just ripped out from SharedPhysicsSystem
        var targetMinTickrate = (float)_config.GetCVar(CVars.TargetMinimumTickrate);
        var serverTickrate = (float)_config.GetCVar(CVars.NetTickrate);
        _substeps = (int)Math.Ceiling(targetMinTickrate / serverTickrate);
        _substepTime = 1.0f / serverTickrate / _substeps;
    }

    private float AABBDistanceSquared(Box2 a, Box2 b)
    {
        var xDist = Math.Max(a.Left - b.Right, b.Left - a.Right);
        var yDist = Math.Max(a.Bottom - b.Top, b.Bottom - a.Top);

        xDist = Math.Max(0, xDist);
        yDist = Math.Max(0, yDist);

        return xDist * xDist + yDist * yDist;
    }

    public virtual (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(EntityUid uid,
        ICommonSession? pSession,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return (EntityCoordinates.Invalid, Angle.Zero);

        // Log.Debug($"Coordinates: {xform.Coordinates}, Angle: {xform.LocalRotation}");
        return (xform.Coordinates, xform.LocalRotation);
    }

    public virtual Angle GetAngle(EntityUid uid, ICommonSession? session, TransformComponent? xform = null)
    {
        var (_, angle) = GetCoordinatesAngle(uid, session, xform);
        return angle;
    }

    public virtual EntityCoordinates GetCoordinates(EntityUid uid,
        ICommonSession? session,
        TransformComponent? xform = null)
    {
        var (coordinates, _) = GetCoordinatesAngle(uid, session, xform);
        return coordinates;
    }

    public EntityCoordinates GetCoordinates(EntityUid uid,
        EntityUid? session,
        TransformComponent? xform = null)
    {
        if (!_actorQuery.TryComp(session, out var actor))
            return GetCoordinates(uid, (ICommonSession?) null, xform);

        return GetCoordinates(uid, actor.PlayerSession, xform);
    }

    public bool IsWithinMargin(Entity<TransformComponent?> sessionEnt, Entity<TransformComponent?> lagCompensatedTarget, ICommonSession? session, float range)
    {
        var targetCoords = GetCoordinates(lagCompensatedTarget, session);
        if (_net.IsServer)
        {
            var targetCurrentCoords = lagCompensatedTarget.Owner.ToCoordinates();
            if (!_transform.InRange(targetCoords, targetCurrentCoords, 0.01f))
                range += MarginTiles;
        }

        return _transform.InRange(sessionEnt.Owner.ToCoordinates(), targetCoords, range);
    }

    public virtual GameTick GetLastRealTick(NetUserId? session)
    {
        return session == null ? _timing.CurTick : _lastRealTicks.GetValueOrDefault(session.Value, _timing.CurTick);
    }

    public void SetLastRealTick(NetUserId session, GameTick tick)
    {
        if (_net.IsClient)
            return;

        _lastRealTicks[session] = tick;
    }

    public void SendLastRealTick()
    {
        if (_net.IsServer)
            return;

        RaiseNetworkEvent(new RMCSetLastRealTickEvent(GetLastRealTick(null)));
    }

    public bool Collides(Entity<FixturesComponent?> target, Entity<PhysicsComponent?> projectile, ICommonSession? perspectiveSession, int substep = 0)
    {
        if (!Resolve(target, ref target.Comp, false) ||
            !Resolve(projectile, ref projectile.Comp, false))
        {
            return false;
        }

        substep = Math.Clamp(substep, -_substeps, _substeps);

        var projectileCoordinates = _transform.GetMapCoordinates(projectile);
        var projectileVelocity = _physics.GetLinearVelocity(projectile, projectile.Comp.LocalCenter);
        var substeppedProjectilePos = projectileCoordinates.Position + (projectileVelocity / _timing.TickRate) * (substep / (float)_substeps);

        var targetCoordinates = _transform.ToMapCoordinates(GetCoordinates(target, perspectiveSession));
        var transform = new Transform(targetCoordinates.Position, 0);
        var targetBounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in target.Comp.Fixtures.Values)
        {
            if ((fixture.CollisionLayer & projectile.Comp.CollisionMask) == 0)
                continue;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                targetBounds = targetBounds.Union(boundy);
            }
        }

        var projectileTransform = new Transform(substeppedProjectilePos, 0);
        var projectileBounds = new Box2(projectileTransform.Position, projectileTransform.Position);

        if (_fixturesQuery.TryComp(projectile, out var projFixtureComp))
        {
            foreach (var fixture in projFixtureComp.Fixtures.Values)
            {
                // TODO RMC14 maybe be more selective on which fixtures to include?
                // Don't think it's a problem right now though.
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var boundy = fixture.Shape.ComputeAABB(projectileTransform, i);
                    projectileBounds = projectileBounds.Union(boundy);
                }
            }
        }

        if (_logPrediction)
        {
            Log.Debug($"""
                Lag comp collide data:
                  Session Name:   {perspectiveSession}
                  Pre-Substep
                    Proj Coords:  {projectileCoordinates}
                  CurTick:        {_timing.CurTick}
                  Substep:        {substep}
                  Projectile Pos: {substeppedProjectilePos}
                  Target Pos:     {targetCoordinates.Position}
                  Proj AABB:      {projectileBounds.BottomLeft}
                                  {projectileBounds.TopRight}
                  Target AABB:    {targetBounds.BottomLeft}
                                  {targetBounds.TopRight}
                  AABB Intersect? {targetBounds.Intersects(projectileBounds)}
                  AABB Distance:  {Math.Sqrt(AABBDistanceSquared(targetBounds, projectileBounds))}
                """);
        }

        if (targetBounds.Intersects(projectileBounds))
        {
            return true;
        }

        if (AABBDistanceSquared(targetBounds, projectileBounds) <= MarginTiles * MarginTiles)
        {
            return true;
        }

        if (_logPrediction)
            Log.Warning($"Predicted hit denied for session '{perspectiveSession}'");

        return false;
    }

    /// <summary>
    /// Returns the current substep the physics system is in.
    /// If physics isn't running, returns null.
    /// </summary>
    /// <returns></returns>
    public int? GetCurrentSubstep()
    {
        if (_physics.EffectiveCurTime is not { } physicsTime)
            return null;

        var diff = physicsTime - _timing.CurTime;
        return (int)Math.Round(diff.TotalSeconds / _substepTime);
    }

    public int GetSubsteps()
    {
        return _substeps;
    }

    /// <summary>
    /// Gets the client's physics substep for purposes of telling the server how much work we've done.
    /// </summary>
    /// <returns>0 if physics isn't running. Current physics substep if it is.</returns>
    public int GetClientSubstep()
    {
        var substep = GetCurrentSubstep();

        if (!substep.HasValue)
            substep = 0; // not in a physics substep

        return substep.Value;
    }
}
