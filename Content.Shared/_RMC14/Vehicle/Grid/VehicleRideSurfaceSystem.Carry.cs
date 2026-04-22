using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleRideSurfaceSystem
{
    private void CarryRiders(
        EntityUid vehicle,
        VehicleRideSurfaceComponent surface,
        RideSurfaceTransform previous,
        RideSurfaceTransform current)
    {
        if (!_ridersByVehicle.TryGetValue(vehicle, out var riders))
            return;

        _riderBuffer.Clear();
        _riderBuffer.AddRange(riders);

        foreach (var rider in _riderBuffer)
        {
            if (_net.IsClient && rider != _player.LocalEntity)
                continue;

            if (!TryComp(rider, out VehicleRideSurfaceRiderComponent? riderComp) ||
                riderComp.Vehicle != vehicle ||
                !_transformQuery.TryComp(rider, out var riderXform))
            {
                UntrackRider(rider, vehicle);
                continue;
            }

            TryCarryRider(vehicle, surface, previous, current, (rider, riderComp), riderXform);
        }

        _riderBuffer.Clear();
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
        if (!Contains(surface, targetLocal, surface.SoftBorderPadding))
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
        _riderBuffer.Clear();
        foreach (var riders in _ridersByVehicle.Values)
            _riderBuffer.AddRange(riders);

        foreach (var uid in _riderBuffer)
        {
            if (!TryComp(uid, out VehicleRideSurfaceRiderComponent? rider) ||
                !_transformQuery.TryComp(uid, out var riderXform))
            {
                if (rider != null)
                    UntrackRider(uid, rider.Vehicle);

                continue;
            }

            if (rider.ClimbDownCompleting)
                continue;

            if (_net.IsClient && uid != _player.LocalEntity)
                continue;

            if (!TryGetValidRideSurface((uid, rider), riderXform, out var surface, out var current))
            {
                ClearRider((uid, rider));
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

            if (!Contains(surface, local, surface.SoftBorderPadding))
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

        _riderBuffer.Clear();
    }

    private void FallOffSurface(
        Entity<VehicleRideSurfaceRiderComponent> rider,
        VehicleRideSurfaceComponent surface)
    {
        if (_net.IsServer)
        {
            if (surface.FallOffKnockdown > TimeSpan.Zero)
                _stun.TryKnockdown(rider.Owner, surface.FallOffKnockdown, true);

            if (surface.FallOffStun > TimeSpan.Zero)
                _stun.TryStun(rider.Owner, surface.FallOffStun, true);
        }

        ClearRider(rider);
    }

    private void ClearRider(Entity<VehicleRideSurfaceRiderComponent> rider, bool cancelDoAfter = true)
    {
        if (cancelDoAfter && rider.Comp.ClimbDownDoAfter is { } doAfter)
            _doAfter.Cancel(doAfter);

        UntrackRider(rider.Owner, rider.Comp.Vehicle);

        rider.Comp.Vehicle = EntityUid.Invalid;
        rider.Comp.ClimbDownDoAfter = null;
        rider.Comp.ClimbDownFromEdge = false;
        rider.Comp.ClimbDownCompleting = false;
        rider.Comp.EdgeClimbDownAt = null;
        Dirty(rider);
        RemCompDeferred<VehicleRideSurfaceRiderComponent>(rider);
    }

    private void SetRiderLocalPosition(Entity<VehicleRideSurfaceRiderComponent> rider, System.Numerics.Vector2 local)
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
}
