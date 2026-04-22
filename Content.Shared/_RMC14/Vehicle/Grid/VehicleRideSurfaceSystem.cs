using System.Numerics;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Vehicle;

public sealed class VehicleRideSurfaceSystem : EntitySystem
{
    private const float TransformPositionEpsilon = 0.000001f;
    private const float TransformRotationEpsilon = 0.0001f;
    private const float ClimbDownExitOffset = 0.1f;
    private const float ClimbDownCancelDistance = 0.2f;
    private const float ClimbOntoSurfaceInset = 0.2f;
    private const float RiderLocalPositionEpsilon = 0.0001f;
    private const float RiderProjectileTargetRadius = 0.75f;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, RideSurfaceTransform> _lastTransforms = new();
    private readonly HashSet<EntityUid> _movedRiders = new();

    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(GridVehicleMoverSystem));

        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<VehicleRideSurfaceComponent, ComponentShutdown>(OnRideSurfaceShutdown);
        SubscribeLocalEvent<VehicleRideSurfaceComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<VehicleRideSurfaceComponent, DoAfterAttemptEvent<VehicleRideSurfaceClimbDoAfterEvent>>(OnClimbDoAfterAttempt);
        SubscribeLocalEvent<VehicleRideSurfaceComponent, VehicleRideSurfaceClimbDoAfterEvent>(OnClimbDoAfter);
        SubscribeLocalEvent<VehicleRideSurfaceComponent, DoAfterAttemptEvent<VehicleRideSurfaceClimbDownDoAfterEvent>>(OnClimbDownDoAfterAttempt);
        SubscribeLocalEvent<VehicleRideSurfaceComponent, VehicleRideSurfaceClimbDownDoAfterEvent>(OnClimbDownDoAfter);
        SubscribeLocalEvent<VehicleRideSurfaceComponent, PreventCollideEvent>(OnSurfacePreventCollide);
        SubscribeLocalEvent<VehicleRideSurfaceRiderComponent, PreventCollideEvent>(OnRiderPreventCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _movedRiders.Clear();

        var query = EntityQueryEnumerator<VehicleRideSurfaceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var surface, out var xform))
        {
            var current = GetRideSurfaceTransform(xform);
            if (surface.Bounds.Count == 0 || current.MapId == MapId.Nullspace)
            {
                ClearSurface(uid);
                continue;
            }

            if (!_lastTransforms.TryGetValue(uid, out var previous) || previous.MapId != current.MapId)
            {
                _lastTransforms[uid] = current;
                continue;
            }

            if (IsSameTransform(previous, current))
                continue;

            CarryRiders(uid, surface, previous, current);
            _lastTransforms[uid] = current;
        }

        ValidateRiders();
    }

    private void OnRideSurfaceShutdown(Entity<VehicleRideSurfaceComponent> ent, ref ComponentShutdown args)
    {
        ClearSurface(ent.Owner);
    }

    private void ClearSurface(EntityUid uid)
    {
        _lastTransforms.Remove(uid);
    }

    private void OnGetAltVerbs(Entity<VehicleRideSurfaceComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || args.Using != null)
            return;

        if (!CanRide(args.Target, ent.Comp, args.User, Transform(args.Target).MapID, out var userXform))
            return;

        if (TryComp(args.User, out VehicleRideSurfaceRiderComponent? rider) &&
            rider.Vehicle == args.Target)
        {
            var climbDownUser = args.User;
            var climbDownTarget = args.Target;
            var climbDownComp = ent.Comp;
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => TryStartClimbDownDoAfter(climbDownUser, climbDownTarget, climbDownComp, breakOnMove: true),
                Text = Loc.GetString("rmc-vehicle-ride-climb-down"),
            });
            return;
        }

        if (!CanClimbOnto(args.User, args.Target, ent.Comp, out _, out _, out _))
            return;

        var user = args.User;
        var target = args.Target;
        var comp = ent.Comp;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryStartClimbDoAfter(user, target, comp),
            Text = Loc.GetString("rmc-vehicle-ride-climb"),
        });
    }

    private void OnClimbDoAfterAttempt(
        Entity<VehicleRideSurfaceComponent> ent,
        ref DoAfterAttemptEvent<VehicleRideSurfaceClimbDoAfterEvent> args)
    {
        if (!CanClimbOnto(args.DoAfter.Args.User, ent.Owner, ent.Comp, out _, out _, out _))
            args.Cancel();
    }

    private void OnClimbDoAfter(Entity<VehicleRideSurfaceComponent> ent, ref VehicleRideSurfaceClimbDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        TryStartRiding(args.User, ent.Owner, ent.Comp);
    }

    private void OnClimbDownDoAfterAttempt(
        Entity<VehicleRideSurfaceComponent> ent,
        ref DoAfterAttemptEvent<VehicleRideSurfaceClimbDownDoAfterEvent> args)
    {
        if (!CanClimbDownFrom(args.DoAfter.Args.User, ent.Owner, ent.Comp))
        {
            args.Cancel();
            return;
        }

    }

    private void OnClimbDownDoAfter(Entity<VehicleRideSurfaceComponent> ent, ref VehicleRideSurfaceClimbDownDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            ClearClimbDownDoAfter(args.User, args.DoAfter.Id);
            return;
        }

        args.Handled = true;
        if (TryComp(args.User, out VehicleRideSurfaceRiderComponent? rider) &&
            rider.ClimbDownDoAfter == args.DoAfter.Id)
        {
            rider.ClimbDownCompleting = true;
        }

        if (TryStopRiding(args.User, ent.Owner, ent.Comp))
            return;

        ClearClimbDownDoAfter(args.User, args.DoAfter.Id);
    }

    private void OnSurfacePreventCollide(Entity<VehicleRideSurfaceComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled ||
            !HasComp<ProjectileComponent>(args.OtherEntity) ||
            !TryComp(args.OtherEntity, out TargetedProjectileComponent? targeted) ||
            !TryComp(targeted.Target, out VehicleRideSurfaceRiderComponent? rider) ||
            rider.Vehicle != ent.Owner)
        {
            return;
        }

        args.Cancelled = true;
    }

    private void OnRiderPreventCollide(Entity<VehicleRideSurfaceRiderComponent> ent, ref PreventCollideEvent args)
    {
        if (args.OtherEntity == ent.Comp.Vehicle)
            args.Cancelled = true;
    }

    public bool TryGetRiderAtCoordinates(EntityUid vehicle, MapCoordinates coordinates, out EntityUid rider)
    {
        rider = default;
        if (!TryComp(vehicle, out VehicleRideSurfaceComponent? surface) ||
            !_transformQuery.HasComp(vehicle))
        {
            return false;
        }

        var current = GetRideSurfaceTransform(_transformQuery.Comp(vehicle));
        if (coordinates.MapId != current.MapId || current.MapId == MapId.Nullspace)
            return false;

        var local = WorldToLocal(coordinates.Position, current);
        if (!Contains(surface, local, surface.ExitPadding))
            return false;

        var radiusSquared = RiderProjectileTargetRadius * RiderProjectileTargetRadius;
        var bestDistance = radiusSquared;
        var found = false;

        var query = EntityQueryEnumerator<VehicleRideSurfaceRiderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var riderComp, out var riderXform))
        {
            if (riderComp.Vehicle != vehicle)
                continue;

            var riderMap = _transform.GetMapCoordinates((uid, riderXform));
            if (riderMap.MapId != coordinates.MapId)
                continue;

            var distance = (riderMap.Position - coordinates.Position).LengthSquared();
            if (distance > bestDistance)
                continue;

            bestDistance = distance;
            rider = uid;
            found = true;
        }

        return found;
    }

    private RideSurfaceTransform GetRideSurfaceTransform(TransformComponent xform)
    {
        var (position, rotation) = _transform.GetWorldPositionRotation(xform, _transformQuery);
        return new RideSurfaceTransform(xform.MapID, position, rotation);
    }

    private bool TryStartClimbDoAfter(EntityUid user, EntityUid vehicle, VehicleRideSurfaceComponent surface)
    {
        if (!CanClimbOnto(user, vehicle, surface, out _, out _, out _))
            return false;

        var climbDelay = surface.ClimbDelay;
        if (climbDelay <= TimeSpan.Zero)
            return TryStartRiding(user, vehicle, surface);

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            climbDelay,
            new VehicleRideSurfaceClimbDoAfterEvent(),
            vehicle,
            target: vehicle)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false,
            RangeCheck = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private bool TryStartClimbDownDoAfter(
        EntityUid user,
        EntityUid vehicle,
        VehicleRideSurfaceComponent surface,
        bool breakOnMove,
        bool fromEdge = false)
    {
        if (!CanClimbDownFrom(user, vehicle, surface))
            return false;

        if (!TryComp(user, out VehicleRideSurfaceRiderComponent? rider))
            return false;

        if (rider.ClimbDownCompleting)
            return true;

        if (TryGetRunningClimbDownDoAfter((user, rider), out _))
            return true;

        var climbDelay = surface.ClimbDownDelay;
        if (climbDelay <= TimeSpan.Zero)
            return TryStopRiding(user, vehicle, surface);

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            climbDelay,
            new VehicleRideSurfaceClimbDownDoAfterEvent(fromEdge),
            vehicle,
            target: vehicle)
        {
            BreakOnMove = breakOnMove,
            BreakOnDamage = true,
            RequireCanInteract = false,
            RangeCheck = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
            CancelDuplicate = false,
        };

        var started = _doAfter.TryStartDoAfter(doAfter, out var doAfterId);
        if (started)
        {
            rider.ClimbDownDoAfter = doAfterId;
            rider.ClimbDownFromEdge = fromEdge;
        }

        return started;
    }

    private bool TryStartRiding(EntityUid user, EntityUid vehicle, VehicleRideSurfaceComponent surface)
    {
        if (!CanClimbOnto(user, vehicle, surface, out var userXform, out var current, out var targetPosition))
            return false;

        var localPosition = WorldToLocal(targetPosition, current);
        _transform.SetMapCoordinates((user, userXform), new MapCoordinates(targetPosition, current.MapId));

        var rider = EnsureComp<VehicleRideSurfaceRiderComponent>(user);
        rider.Vehicle = vehicle;
        rider.LocalPosition = localPosition;
        rider.ClimbDownDoAfter = null;
        rider.ClimbDownFromEdge = false;
        rider.ClimbDownCompleting = false;
        rider.EdgeClimbDownAt = null;
        Dirty(user, rider);

        var vehicleIdentity = Identity.Entity(vehicle, EntityManager);
        var selfMessage = Loc.GetString("rmc-vehicle-ride-climb-self", ("vehicle", vehicleIdentity));
        var othersMessage = Loc.GetString(
            "rmc-vehicle-ride-climb-others",
            ("user", Identity.Entity(user, EntityManager)),
            ("vehicle", vehicleIdentity));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        return true;
    }

    private bool TryStopRiding(EntityUid user, EntityUid vehicle, VehicleRideSurfaceComponent surface)
    {
        if (!CanClimbDownFrom(user, vehicle, surface))
            return false;

        var current = GetRideSurfaceTransform(Transform(vehicle));
        var moved = false;
        var targetPosition = Vector2.Zero;
        if (current.MapId != MapId.Nullspace &&
            _transformQuery.TryComp(user, out var userXform))
        {
            var userMap = _transform.GetMapCoordinates((user, userXform));
            targetPosition = userMap.Position;
            if (TryGetClimbDownPosition(surface, current, userMap.Position, out targetPosition))
            {
                var local = WorldToLocal(userMap.Position, current);
                if (Contains(surface, local, 0f))
                {
                    _transform.SetMapCoordinates((user, userXform), new MapCoordinates(targetPosition, current.MapId));
                    moved = true;
                }
            }
        }

        if (TryComp(user, out VehicleRideSurfaceRiderComponent? rider))
            ClearRider((user, rider), cancelDoAfter: false);
        else
            RemCompDeferred<VehicleRideSurfaceRiderComponent>(user);

        var vehicleIdentity = Identity.Entity(vehicle, EntityManager);
        var selfMessage = Loc.GetString("rmc-vehicle-ride-climb-down-self", ("vehicle", vehicleIdentity));
        var othersMessage = Loc.GetString(
            "rmc-vehicle-ride-climb-down-others",
            ("user", Identity.Entity(user, EntityManager)),
            ("vehicle", vehicleIdentity));
        _popup.PopupPredicted(selfMessage, othersMessage, user, user);

        return true;
    }

    private bool CanClimbOnto(
        EntityUid user,
        EntityUid vehicle,
        VehicleRideSurfaceComponent surface,
        out TransformComponent userXform,
        out RideSurfaceTransform current,
        out Vector2 targetPosition)
    {
        userXform = default!;
        current = default;
        targetPosition = default;

        if (!CanRide(vehicle, surface, user, Transform(vehicle).MapID, out userXform))
            return false;

        current = GetRideSurfaceTransform(Transform(vehicle));
        if (current.MapId == MapId.Nullspace)
            return false;

        var userMap = _transform.GetMapCoordinates((user, userXform));
        if (userMap.MapId != current.MapId)
            return false;

        var userLocal = WorldToLocal(userMap.Position, current);
        if (!CanStartClimbFrom(surface, userLocal))
            return false;

        if (!TryGetClimbOntoSurfacePoint(surface, current, userMap.Position, out targetPosition))
            return false;

        var climbRange = MathF.Max(0f, surface.ClimbRange);
        return (targetPosition - userMap.Position).LengthSquared() <= climbRange * climbRange;
    }

    private bool CanClimbDownFrom(EntityUid user, EntityUid vehicle, VehicleRideSurfaceComponent surface)
    {
        if (!TryComp(user, out VehicleRideSurfaceRiderComponent? rider) || rider.Vehicle != vehicle)
            return false;

        if (!CanRide(vehicle, surface, user, Transform(vehicle).MapID, out var userXform))
            return false;

        if (!TryGetValidRideSurface((user, rider), userXform, out _, out _))
            return false;

        return true;
    }

    private void CarryRiders(
        EntityUid vehicle,
        VehicleRideSurfaceComponent surface,
        RideSurfaceTransform previous,
        RideSurfaceTransform current)
    {
        var riders = EntityQueryEnumerator<VehicleRideSurfaceRiderComponent, TransformComponent>();
        while (riders.MoveNext(out var rider, out var riderComp, out var riderXform))
        {
            if (riderComp.Vehicle != vehicle)
                continue;

            if (_net.IsClient && rider != _player.LocalEntity)
                continue;

            TryCarryRider(vehicle, surface, previous, current, (rider, riderComp), riderXform);
        }
    }

    private bool TryCarryRider(
        EntityUid vehicle,
        VehicleRideSurfaceComponent surface,
        RideSurfaceTransform previous,
        RideSurfaceTransform current,
        Entity<VehicleRideSurfaceRiderComponent> rider,
        TransformComponent riderXform)
    {
        if (_movedRiders.Contains(rider.Owner))
            return false;

        if (!CanRide(vehicle, surface, rider.Owner, current.MapId, out _))
            return false;

        var riderMap = _transform.GetMapCoordinates((rider.Owner, riderXform));
        var mapLocalOnPreviousSurface = WorldToLocal(riderMap.Position, previous);
        var riderMovement = mapLocalOnPreviousSurface - rider.Comp.LocalPosition;
        var targetLocal = rider.Comp.LocalPosition + riderMovement;
        if (!Contains(surface, targetLocal, surface.ExitPadding))
            return false;

        var targetPosition = LocalToWorld(targetLocal, current);
        var delta = targetPosition - riderMap.Position;
        var maxCarryDistance = MathF.Max(0f, surface.MaxCarryDistance);
        if (delta.LengthSquared() > maxCarryDistance * maxCarryDistance)
            return false;

        if (delta.LengthSquared() > 0.000001f)
            _transform.SetMapCoordinates((rider.Owner, riderXform), new MapCoordinates(targetPosition, current.MapId));

        SetRiderLocalPosition(rider, targetLocal);

        _movedRiders.Add(rider.Owner);
        return true;
    }

    private void ValidateRiders()
    {
        var query = EntityQueryEnumerator<VehicleRideSurfaceRiderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rider, out var riderXform))
        {
            if (rider.ClimbDownCompleting)
                continue;

            if (_net.IsClient && uid != _player.LocalEntity)
                continue;

            if (!TryGetValidRideSurface((uid, rider), riderXform, out var surface, out var current))
            {
                RemCompDeferred<VehicleRideSurfaceRiderComponent>(uid);
                continue;
            }

            var riderMap = _transform.GetMapCoordinates((uid, riderXform));
            var local = WorldToLocal(riderMap.Position, current);
            if (Contains(surface, local, 0f))
            {
                rider.EdgeClimbDownAt = null;
                SetRiderLocalPosition((uid, rider), local);

                if (rider.ClimbDownFromEdge &&
                    TryGetRunningClimbDownDoAfter((uid, rider), out var doAfterId))
                {
                    var distanceToEdge = GetDistanceToClosestSurfaceEdge(surface, local);
                    if (distanceToEdge > ClimbDownCancelDistance)
                    {
                        _doAfter.Cancel(doAfterId);
                        rider.ClimbDownDoAfter = null;
                        rider.ClimbDownFromEdge = false;
                    }
                }

                continue;
            }

            if (!Contains(surface, local, surface.ExitPadding))
            {
                FallOffSurface((uid, rider), surface);
                continue;
            }

            var grace = Math.Max(0, surface.EdgeClimbDownGrace.TotalSeconds);
            var edgeClimbDownAt = rider.EdgeClimbDownAt ?? _timing.CurTime + TimeSpan.FromSeconds(grace);
            rider.EdgeClimbDownAt = edgeClimbDownAt;
            if (edgeClimbDownAt <= _timing.CurTime)
                TryStartClimbDownDoAfter(uid, rider.Vehicle, surface, breakOnMove: false, fromEdge: true);

            SetRiderLocalPosition((uid, rider), local);
        }
    }

    private void FallOffSurface(
        Entity<VehicleRideSurfaceRiderComponent> rider,
        VehicleRideSurfaceComponent surface)
    {
        if (_net.IsServer && surface.FallOffStun > TimeSpan.Zero)
            _stun.TryParalyze(rider.Owner, surface.FallOffStun, true);

        ClearRider(rider);
    }

    private bool TryGetRunningClimbDownDoAfter(
        Entity<VehicleRideSurfaceRiderComponent> rider,
        out DoAfterId doAfterId)
    {
        doAfterId = default;
        if (rider.Comp.ClimbDownDoAfter is not { } active)
            return false;

        var status = _doAfter.GetStatus(active);
        if (status == DoAfterStatus.Running)
        {
            doAfterId = active;
            return true;
        }

        if (status == DoAfterStatus.Finished)
        {
            rider.Comp.ClimbDownCompleting = true;
            rider.Comp.EdgeClimbDownAt = null;
            doAfterId = active;
            return true;
        }

        rider.Comp.ClimbDownDoAfter = null;
        rider.Comp.ClimbDownFromEdge = false;
        rider.Comp.ClimbDownCompleting = false;
        rider.Comp.EdgeClimbDownAt = null;
        return false;
    }

    private void ClearClimbDownDoAfter(EntityUid user, DoAfterId doAfterId)
    {
        if (!TryComp(user, out VehicleRideSurfaceRiderComponent? rider) ||
            rider.ClimbDownDoAfter != doAfterId)
        {
            return;
        }

        rider.ClimbDownDoAfter = null;
        rider.ClimbDownFromEdge = false;
        rider.ClimbDownCompleting = false;
        rider.EdgeClimbDownAt = null;
    }

    private void ClearRider(Entity<VehicleRideSurfaceRiderComponent> rider, bool cancelDoAfter = true)
    {
        if (cancelDoAfter && rider.Comp.ClimbDownDoAfter is { } doAfter)
            _doAfter.Cancel(doAfter);

        rider.Comp.Vehicle = EntityUid.Invalid;
        rider.Comp.ClimbDownDoAfter = null;
        rider.Comp.ClimbDownFromEdge = false;
        rider.Comp.ClimbDownCompleting = false;
        rider.Comp.EdgeClimbDownAt = null;
        Dirty(rider);
        RemCompDeferred<VehicleRideSurfaceRiderComponent>(rider);
    }

    private void SetRiderLocalPosition(Entity<VehicleRideSurfaceRiderComponent> rider, Vector2 local)
    {
        if (_net.IsClient)
            return;

        if ((rider.Comp.LocalPosition - local).LengthSquared() <= RiderLocalPositionEpsilon)
            return;

        rider.Comp.LocalPosition = local;
        Dirty(rider);
    }

    private bool TryGetValidRideSurface(
        Entity<VehicleRideSurfaceRiderComponent> rider,
        TransformComponent riderXform,
        out VehicleRideSurfaceComponent surface,
        out RideSurfaceTransform current)
    {
        surface = default!;
        current = default;

        var vehicle = rider.Comp.Vehicle;
        if (!vehicle.IsValid() || TerminatingOrDeleted(vehicle))
            return false;

        if (!TryComp<VehicleRideSurfaceComponent>(vehicle, out var surfaceComp) || !_transformQuery.HasComp(vehicle))
            return false;

        surface = surfaceComp;
        current = GetRideSurfaceTransform(_transformQuery.Comp(vehicle));
        if (current.MapId == MapId.Nullspace || riderXform.MapID != current.MapId)
            return false;

        if (!CanRide(vehicle, surface, rider.Owner, current.MapId, out _))
            return false;

        return true;
    }

    private bool CanRide(
        EntityUid vehicle,
        VehicleRideSurfaceComponent surface,
        EntityUid rider,
        MapId mapId,
        out TransformComponent riderXform)
    {
        riderXform = default!;

        if (rider == vehicle || TerminatingOrDeleted(rider))
            return false;

        if (_net.IsClient && rider != _player.LocalEntity)
            return false;

        if (!_mobStateQuery.HasComp(rider))
            return false;

        if (!_transformQuery.HasComp(rider))
            return false;

        riderXform = _transformQuery.Comp(rider);

        if (riderXform.MapID != mapId || riderXform.Anchored)
            return false;

        if (!surface.CarryBuckled &&
            _buckleQuery.TryComp(rider, out var buckle) &&
            buckle.Buckled)
        {
            return false;
        }

        return true;
    }

    private static bool Contains(VehicleRideSurfaceComponent surface, Vector2 localPoint, float padding)
    {
        padding = MathF.Max(0f, padding);
        foreach (var bounds in surface.Bounds)
        {
            if (bounds.IsValid() && bounds.Enlarged(padding).Contains(localPoint))
                return true;
        }

        return false;
    }

    private static bool CanStartClimbFrom(VehicleRideSurfaceComponent surface, Vector2 localPoint)
    {
        if (surface.ClimbBounds.Count == 0)
            return Contains(surface, localPoint, MathF.Max(0f, surface.ClimbRange));

        foreach (var bounds in surface.ClimbBounds)
        {
            if (bounds.IsValid() && bounds.Contains(localPoint))
                return true;
        }

        return false;
    }

    private static bool TryGetClosestSurfaceLocal(
        VehicleRideSurfaceComponent surface,
        Vector2 localPoint,
        out Vector2 closestLocal)
    {
        return TryGetClosestSurfaceLocal(surface, localPoint, out closestLocal, out _);
    }

    private static bool TryGetClosestSurfaceLocal(
        VehicleRideSurfaceComponent surface,
        Vector2 localPoint,
        out Vector2 closestLocal,
        out Box2 closestBounds)
    {
        closestLocal = default;
        closestBounds = default;
        var closestDistance = float.MaxValue;
        var found = false;

        foreach (var bounds in surface.Bounds)
        {
            if (!bounds.IsValid())
                continue;

            var clamped = new Vector2(
                Math.Clamp(localPoint.X, bounds.Left, bounds.Right),
                Math.Clamp(localPoint.Y, bounds.Bottom, bounds.Top));
            var distance = (clamped - localPoint).LengthSquared();
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestLocal = clamped;
            closestBounds = bounds;
            found = true;
        }

        return found;
    }

    private static float GetDistanceToClosestSurfaceEdge(VehicleRideSurfaceComponent surface, Vector2 localPoint)
    {
        var closestDistance = float.MaxValue;

        foreach (var bounds in surface.Bounds)
        {
            if (!bounds.IsValid() || !bounds.Contains(localPoint))
                continue;

            closestDistance = Math.Min(closestDistance, MathF.Abs(localPoint.X - bounds.Left));
            closestDistance = Math.Min(closestDistance, MathF.Abs(bounds.Right - localPoint.X));
            closestDistance = Math.Min(closestDistance, MathF.Abs(localPoint.Y - bounds.Bottom));
            closestDistance = Math.Min(closestDistance, MathF.Abs(bounds.Top - localPoint.Y));
        }

        return closestDistance == float.MaxValue ? 0f : closestDistance;
    }

    private static bool TryGetClimbOntoSurfacePoint(
        VehicleRideSurfaceComponent surface,
        RideSurfaceTransform transform,
        Vector2 worldPoint,
        out Vector2 targetPosition)
    {
        targetPosition = default;
        var localPoint = WorldToLocal(worldPoint, transform);
        if (!TryGetClosestSurfaceLocal(surface, localPoint, out var closestLocal, out var closestBounds))
            return false;

        targetPosition = LocalToWorld(InsetIntoSurface(closestLocal, closestBounds), transform);
        return true;
    }

    private static Vector2 InsetIntoSurface(Vector2 localPoint, Box2 bounds)
    {
        var toCenter = bounds.Center - localPoint;
        var distance = toCenter.Length();
        if (distance <= 0.0001f)
            return localPoint;

        var target = localPoint + toCenter / distance * MathF.Min(ClimbOntoSurfaceInset, distance);
        return new Vector2(
            Math.Clamp(target.X, bounds.Left, bounds.Right),
            Math.Clamp(target.Y, bounds.Bottom, bounds.Top));
    }

    private static bool TryGetClimbDownPosition(
        VehicleRideSurfaceComponent surface,
        RideSurfaceTransform transform,
        Vector2 worldPoint,
        out Vector2 targetPosition)
    {
        targetPosition = worldPoint;
        var localPoint = WorldToLocal(worldPoint, transform);
        var closestDistance = float.MaxValue;
        var found = false;

        foreach (var bounds in surface.Bounds)
        {
            if (!bounds.IsValid())
                continue;

            var clamped = new Vector2(
                Math.Clamp(localPoint.X, bounds.Left, bounds.Right),
                Math.Clamp(localPoint.Y, bounds.Bottom, bounds.Top));

            var leftDistance = MathF.Abs(clamped.X - bounds.Left);
            var rightDistance = MathF.Abs(bounds.Right - clamped.X);
            var bottomDistance = MathF.Abs(clamped.Y - bounds.Bottom);
            var topDistance = MathF.Abs(bounds.Top - clamped.Y);

            var distance = Math.Min(Math.Min(leftDistance, rightDistance), Math.Min(bottomDistance, topDistance));
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            found = true;

            var exitPadding = MathF.Max(0f, surface.ExitPadding) + ClimbDownExitOffset;
            if (distance == leftDistance)
                clamped.X = bounds.Left - exitPadding;
            else if (distance == rightDistance)
                clamped.X = bounds.Right + exitPadding;
            else if (distance == bottomDistance)
                clamped.Y = bounds.Bottom - exitPadding;
            else
                clamped.Y = bounds.Top + exitPadding;

            targetPosition = LocalToWorld(clamped, transform);
        }

        return found;
    }

    private static Vector2 WorldToLocal(Vector2 world, RideSurfaceTransform transform)
    {
        return (-transform.Rotation).RotateVec(world - transform.Position);
    }

    private static Vector2 LocalToWorld(Vector2 local, RideSurfaceTransform transform)
    {
        return transform.Position + transform.Rotation.RotateVec(local);
    }

    private static bool IsSameTransform(RideSurfaceTransform previous, RideSurfaceTransform current)
    {
        return (current.Position - previous.Position).LengthSquared() <= TransformPositionEpsilon &&
               MathF.Abs((float) (current.Rotation - previous.Rotation).Theta) <= TransformRotationEpsilon;
    }

    private readonly record struct RideSurfaceTransform(MapId MapId, Vector2 Position, Angle Rotation);
}
