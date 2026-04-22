using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleRideSurfaceSystem
{
    private void OnGetAltVerbs(Entity<VehicleRideSurfaceComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || args.Using != null)
            return;

        if (!CanRide(args.Target, ent.Comp, args.User, Transform(args.Target).MapID, out _))
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
            args.Cancel();
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
        if (rider.Vehicle != vehicle)
            UntrackRider(user, rider.Vehicle);

        rider.Vehicle = vehicle;
        rider.LocalPosition = localPosition;
        rider.ClimbDownDoAfter = null;
        rider.ClimbDownFromEdge = false;
        rider.ClimbDownCompleting = false;
        rider.EdgeClimbDownAt = null;
        Dirty(user, rider);
        TrackRider(vehicle, user);

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
        if (current.MapId != MapId.Nullspace &&
            _transformQuery.TryComp(user, out var userXform))
        {
            var userMap = _transform.GetMapCoordinates((user, userXform));
            if (TryGetClimbDownPosition(surface, current, userMap.Position, out var targetPosition))
            {
                var local = WorldToLocal(userMap.Position, current);
                if (Contains(surface, local, 0f))
                    _transform.SetMapCoordinates((user, userXform), new MapCoordinates(targetPosition, current.MapId));
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
}
