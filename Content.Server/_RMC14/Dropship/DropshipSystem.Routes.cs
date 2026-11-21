using Content.Shared.Shuttles.Components;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Thunderdome;

namespace Content.Server._RMC14.Dropship;

public sealed partial class DropshipSystem
{
    private bool TryResolveRemoteLinkedLandingZone(
        Entity<DropshipRemoteControlConsoleComponent> console,
        out Entity<DropshipDestinationComponent> landingZone)
    {
        if (console.Comp.LinkedLandingZone is { } linked &&
            !TerminatingOrDeleted(linked) &&
            TryComp(linked, out DropshipDestinationComponent? linkedDestination) &&
            ResolveDestinationKind((linked, linkedDestination)) == DropshipDestinationKind.LandingZone)
        {
            landingZone = (linked, linkedDestination);
            return true;
        }

        if (FindClosestRemoteLandingZone(console.Owner) is { } closest)
        {
            landingZone = (closest.Owner, closest.Comp);
            return true;
        }

        landingZone = default;
        return false;
    }

    private Entity<DropshipDestinationComponent>? FindClosestRemoteLandingZone(EntityUid console)
    {
        if (!TryComp(console, out TransformComponent? consoleTransform))
            return null;

        Entity<DropshipDestinationComponent>? closestLandingZone = null;
        var closestDistance = 0f;
        var destinations = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
        while (destinations.MoveNext(out var uid, out var destination, out var transform))
        {
            if (transform.MapID != consoleTransform.MapID ||
                ResolveDestinationKind((uid, destination)) != DropshipDestinationKind.LandingZone ||
                !consoleTransform.Coordinates.TryDistance(EntityManager, transform.Coordinates, out var distance))
            {
                continue;
            }

            if (closestLandingZone == null || distance < closestDistance)
            {
                closestLandingZone = (uid, destination);
                closestDistance = distance;
            }
        }

        return closestLandingZone;
    }

    private bool IsRemoteControlConsolePlanetside(EntityUid console)
    {
        var xform = Transform(console);
        return HasComp<RMCPlanetComponent>(xform.GridUid) ||
               HasComp<RMCPlanetComponent>(xform.MapUid);
    }

    private bool TrySummonAnyAvailableDropship(
        EntityUid user,
        Entity<DropshipDestinationComponent> destination)
    {
        var dropships = EntityQueryEnumerator<DropshipComponent, TransformComponent>();
        while (dropships.MoveNext(out var uid, out var dropship, out var xform))
        {
            if (HasComp<ThunderdomeMapComponent>(xform.MapUid))
                continue;

            if (!TryGetDropshipComputer(uid, out var computer) ||
                !CanUseDestinationForRemoteOrAutopilot(
                    computer,
                    (uid, dropship),
                    destination,
                    DropshipDestinationKind.LandingZone,
                    true,
                    out _))
            {
                continue;
            }

            if (FlyTo(computer, destination.Owner, user, source: DropshipLaunchSource.PlanetsideTerminal))
                return true;
        }

        return false;
    }

    private bool TryGetAutopilotDropship(
        NetEntity computerNet,
        out Entity<DropshipNavigationComputerComponent> computer,
        out Entity<DropshipComponent> dropship)
    {
        computer = default;
        dropship = default;

        if (!TryGetEntity(computerNet, out var computerId) ||
            !TryComp(computerId, out DropshipNavigationComputerComponent? computerComp) ||
            !computerComp.Hijackable)
        {
            return false;
        }

        computer = (computerId.Value, computerComp);
        return !TerminatingOrDeleted(computerId.Value) && TryGetGridDropship(computer, out dropship) &&
               !TerminatingOrDeleted(dropship.Owner) &&
               !HasComp<ThunderdomeMapComponent>(Transform(dropship).MapUid);
    }

    private bool TryGetDropshipComputer(EntityUid dropship, out Entity<DropshipNavigationComputerComponent> computer)
    {
        computer = default;
        if (TerminatingOrDeleted(dropship) || HasComp<ThunderdomeMapComponent>(Transform(dropship).MapUid))
            return false;

        var children = Transform(dropship).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (TerminatingOrDeleted(child) || !TryComp(child, out DropshipNavigationComputerComponent? computerComp) ||
                !computerComp.Hijackable)
            {
                continue;
            }

            computer = (child, computerComp);
            return true;
        }

        return false;
    }

    private DropshipDestinationKind ResolveDestinationKind(Entity<DropshipDestinationComponent> destination)
    {
        if (destination.Comp.Kind != DropshipDestinationKind.Auto)
            return destination.Comp.Kind;

        if (destination.Comp.Spawn != null)
            return DropshipDestinationKind.Hangar;

        var xform = Transform(destination);
        if (HasComp<RMCPlanetComponent>(xform.GridUid) ||
            HasComp<RMCPlanetComponent>(xform.MapUid))
        {
            return DropshipDestinationKind.LandingZone;
        }

        if (HasComp<AlmayerComponent>(xform.GridUid) ||
            HasComp<AlmayerComponent>(xform.MapUid))
        {
            return DropshipDestinationKind.Hangar;
        }

        return DropshipDestinationKind.Auto;
    }

    private bool TryResolveRouteHangar(
        Entity<DropshipComponent> dropship,
        DropshipAutopilotComponent autopilot,
        out EntityUid routeHangar)
    {
        if (TryResolveDestinationKind(autopilot.RouteHangar, DropshipDestinationKind.Hangar, out routeHangar))
            return true;

        if (TryResolveDestinationKind(dropship.Comp.Destination, DropshipDestinationKind.Hangar, out routeHangar))
        {
            autopilot.RouteHangar = routeHangar;
            Dirty(dropship.Owner, autopilot);
            return true;
        }

        var query = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (query.MoveNext(out var uid, out var destination))
        {
            if (destination.Ship != dropship.Owner ||
                ResolveDestinationKind((uid, destination)) != DropshipDestinationKind.Hangar)
            {
                continue;
            }

            routeHangar = uid;
            autopilot.RouteHangar = routeHangar;
            Dirty(dropship.Owner, autopilot);
            return true;
        }

        routeHangar = default;
        return false;
    }

    private bool TryResolveLandingZone(DropshipAutopilotComponent autopilot, out EntityUid landingZone)
    {
        return TryResolveDestinationKind(autopilot.LandingZone, DropshipDestinationKind.LandingZone, out landingZone);
    }

    private bool TryResolveDestinationKind(EntityUid? destination, DropshipDestinationKind kind, out EntityUid resolved)
    {
        if (destination is { } uid &&
            !TerminatingOrDeleted(uid) &&
            TryComp(uid, out DropshipDestinationComponent? destinationComp) &&
            ResolveDestinationKind((uid, destinationComp)) == kind)
        {
            resolved = uid;
            return true;
        }

        resolved = default;
        return false;
    }

    private bool CanAutopilotLaunchTo(
        Entity<DropshipNavigationComputerComponent> computer,
        Entity<DropshipComponent> dropship,
        EntityUid target,
        out string reason)
    {
        if (!TryComp(target, out DropshipDestinationComponent? destination))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-invalid");
            return false;
        }

        var kind = ResolveDestinationKind((target, destination));
        if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-invalid");
            return false;
        }

        return CanUseDestinationForRemoteOrAutopilot(
            computer,
            dropship,
            (target, destination),
            kind,
            true,
            out reason);
    }

    private bool CanRemoteLaunchTo(
        Entity<DropshipNavigationComputerComponent> computer,
        Entity<DropshipComponent> dropship,
        Entity<DropshipDestinationComponent> target,
        out string reason)
    {
        var kind = ResolveDestinationKind(target);
        if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        return CanUseDestinationForRemoteOrAutopilot(
            computer,
            dropship,
            target,
            kind,
            true,
            out reason);
    }

    private bool CanUseDestinationForRemoteOrAutopilot(
        Entity<DropshipNavigationComputerComponent> computer,
        Entity<DropshipComponent> dropship,
        Entity<DropshipDestinationComponent> destination,
        DropshipDestinationKind requiredKind,
        bool requireLaunchReady,
        out string reason)
    {
        if (TerminatingOrDeleted(destination) || Transform(destination).MapUid == null)
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        var kind = ResolveDestinationKind(destination);
        if (requiredKind != DropshipDestinationKind.Auto && kind != requiredKind)
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        if (Transform(computer.Owner).GridUid != dropship.Owner)
        {
            reason = Loc.GetString("rmc-dropship-remote-error-invalid-destination");
            return false;
        }

        if (!requireLaunchReady)
        {
            reason = string.Empty;
            return true;
        }

        if (_timing.CurTime < computer.Comp.LockedOutUntil)
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-locked");
            return false;
        }

        return CanLaunchToDestinationNow(dropship, destination, out reason);
    }

    private bool CanLaunchToDestinationNow(
        Entity<DropshipComponent> dropship,
        Entity<DropshipDestinationComponent> destination,
        out string reason)
    {
        if (dropship.Comp.Crashed)
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-crashed");
            return false;
        }

        if (HasComp<FTLComponent>(dropship.Owner))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-ftl");
            return false;
        }

        if (IsDropshipPreFlightFueling(out var remaining))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-preflight",
                ("minutes", Math.Max(1, (int)remaining.TotalMinutes)));
            return false;
        }

        if (dropship.Comp.Destination == destination.Owner)
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-same");
            return false;
        }

        if (IsDestinationOccupiedByOther(dropship, destination.Comp))
        {
            reason = Loc.GetString("rmc-dropship-autopilot-status-detail-target-occupied");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsDestinationOccupiedByOther(Entity<DropshipComponent> dropship, DropshipDestinationComponent destination)
    {
        return destination.Ship is { } occupiedBy && occupiedBy != dropship.Owner;
    }
}
