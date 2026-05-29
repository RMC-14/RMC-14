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
    private int _substeps;
    private float _substepTime;
    // This contains _substeps + 1 items and is just an array of ratios
    // representing how far into a frame each substep is. E.g. if there are four substeps
    // the array would be [0.0f, 0.25f, 0.5f, 0.75f, 1.0f]
    private float[] _substepMults = [];
    private bool _logPrediction = false;

    private readonly Dictionary<NetUserId, GameTick> _lastRealTicks = new();


    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();

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

        // Division is slow so we pre-calculate the multipliers we need here.
        _substepMults = new float[_substeps + 1];
        for (var i = 0; i <= _substeps; i++)
            _substepMults[i] = (float)i / _substeps;
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

    public bool Collides(Entity<FixturesComponent?> target, Entity<PhysicsComponent?> projectile, MapCoordinates targetCoordinates, int substep = 0)
    {
        if (!Resolve(target, ref target.Comp, false) ||
            !Resolve(projectile, ref projectile.Comp, false))
        {
            return false;
        }

        substep = Math.Clamp(substep, 0, _substeps);

        var projectileCoordinates = _transform.GetMapCoordinates(projectile);
        var projectileVelocity = _physics.GetLinearVelocity(projectile, projectile.Comp.LocalCenter);
        var substeppedProjectilePos = projectileCoordinates.Position + (projectileVelocity / _timing.TickRate) * _substepMults[substep];

        var transform = new Transform(targetCoordinates.Position, 0);
        var bounds = new Box2(transform.Position, transform.Position);

        foreach (var fixture in target.Comp.Fixtures.Values)
        {
            if ((fixture.CollisionLayer & projectile.Comp.CollisionMask) == 0)
                continue;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var boundy = fixture.Shape.ComputeAABB(transform, i);
                bounds = bounds.Union(boundy);
            }
        }

        if (_logPrediction)
        {
            Log.Debug($"""
                Lag comp collide data:
                  Pre-Substep Projectile Coords: {projectileCoordinates}
                  Substep: {substep}
                  Projectile Pos: {substeppedProjectilePos}
                  Target Pos:     {targetCoordinates.Position}
                  Target AABB:
                    {bounds.BottomLeft}
                    {bounds.TopRight}
                  Contained in AABB? {bounds.Contains(substeppedProjectilePos)}
                  Closest point diff: {(bounds.ClosestPoint(substeppedProjectilePos) - substeppedProjectilePos).Length()}
                """);
        }

        if (bounds.Contains(substeppedProjectilePos))
        {
            return true;
        }

        var closest = bounds.ClosestPoint(substeppedProjectilePos);
        if ((closest - substeppedProjectilePos).LengthSquared() <= MarginTiles * MarginTiles)
        {
            return true;
        }

        if (_logPrediction)
            Log.Warning("Predicted hit denied.");

        return false;
    }

    public bool Collides(Entity<FixturesComponent?> target, Entity<PhysicsComponent?> projectile, ICommonSession session, int substep = 0)
    {
        var coordinates = _transform.ToMapCoordinates(GetCoordinates(target, session));
        return Collides(target, projectile, coordinates, substep);
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
    /// <returns>0 if physics isn't running. 1 to GetSubsteps() if physics is running.</returns>
    public int GetClientSubstep()
    {
        var substep = GetCurrentSubstep();

        // I know this is necessary from testing but I can't explain why. For some reason
        // the server is a full physics step behind us but only for the very first substep.
        // If we're not in a physics substep the server will be aligned with us.
        if (!substep.HasValue)
            substep = 0; // not in a physics substep
        else if (substep == 0)
            substep = _substeps; // first physics substep special case

        return substep.Value;
    }
}
