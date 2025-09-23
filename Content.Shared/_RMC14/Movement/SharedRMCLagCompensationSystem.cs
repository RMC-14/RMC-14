using Content.Shared._RMC14.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Movement;

public abstract class SharedRMCLagCompensationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public float MarginTiles { get; private set; }

    private EntityQuery<ActorComponent> _actorQuery;

    private readonly Dictionary<NetUserId, GameTick> _lastRealTicks = new();

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeNetworkEvent<RMCSetLastRealTickEvent>(OnSetLastRealTick);

        Subs.CVar(_config, RMCCVars.RMCLagCompensationMarginTiles, v => MarginTiles = v, true);
    }

    private void OnSetLastRealTick(RMCSetLastRealTickEvent msg, EntitySessionEventArgs args)
    {
        SetLastRealTick(args.SenderSession.UserId, msg.Tick - 1);
    }

    public virtual (EntityCoordinates Coordinates, Angle Angle) GetCoordinatesAngle(EntityUid uid,
        ICommonSession? pSession,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return (EntityCoordinates.Invalid, Angle.Zero);

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

    public virtual EntityCoordinates GetCoordinates(EntityUid uid,
        EntityUid? session,
        TransformComponent? xform = null)
    {
        if (!_actorQuery.TryComp(session, out var actor))
            return GetCoordinates(uid, (ICommonSession?) null, xform);

        return GetCoordinates(uid, actor.PlayerSession, xform);
    }

    public bool IsWithinMargin(Entity<TransformComponent?> one, Entity<TransformComponent?> two, ICommonSession? session, float range)
    {
        if (_net.IsServer)
            range += MarginTiles;

        return _transform.InRange(GetCoordinates(one, session), GetCoordinates(two, session), range);
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
        if (_net.IsClient)
            return;

        RaiseNetworkEvent(new RMCSetLastRealTickEvent(GetLastRealTick(null)));
    }

    public bool Collides(Entity<FixturesComponent?> target, Entity<PhysicsComponent?> projectile, MapCoordinates targetCoordinates)
    {
        if (!Resolve(target, ref target.Comp, false) ||
            !Resolve(projectile, ref projectile.Comp, false))
        {
            return false;
        }

        var projectileCoordinates = _transform.GetMapCoordinates(projectile);
        var projectilePosition = projectileCoordinates.Position;
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

        if (bounds.Contains(projectilePosition))
            return true;

        var closest = bounds.ClosestPoint(projectilePosition);
        if ((closest - projectilePosition).LengthSquared() <= MarginTiles * MarginTiles)
            return true;

        return false;
    }
}
