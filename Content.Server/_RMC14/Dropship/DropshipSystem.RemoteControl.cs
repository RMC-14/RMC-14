using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared._RMC14.Dropship;

namespace Content.Server._RMC14.Dropship;

public sealed partial class DropshipSystem
{
    private DropshipNavigationAutopilotStatus? GetNavigationAutopilotStatus(EntityUid dropship)
    {
        if (!TryComp(dropship, out DropshipAutopilotComponent? autopilot) ||
            autopilot.Mode == DropshipAutopilotMode.Disabled)
        {
            return null;
        }

        return new DropshipNavigationAutopilotStatus(
            autopilot.Mode,
            autopilot.Status,
            autopilot.StatusDetails,
            GetAutopilotDepartInSeconds(autopilot));
    }

    private int? GetAutopilotDepartInSeconds(DropshipAutopilotComponent autopilot)
    {
        if (autopilot.NextDepartureAt is not { } departAt)
            return null;

        return Math.Max(0, (int)Math.Ceiling((departAt - _timing.CurTime).TotalSeconds));
    }

    private void RefreshRemoteControlConsoles()
    {
        var consoles = EntityQueryEnumerator<DropshipRemoteControlConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            RefreshRemoteControlConsole((uid, console));
        }
    }

    private void RefreshRemoteControlConsole(Entity<DropshipRemoteControlConsoleComponent> console)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key))
            return;

        var controllableDropships = new List<(EntityUid Uid, DropshipComponent Dropship, Entity<DropshipNavigationComputerComponent> Computer)>();
        var dropships = new List<DropshipRemoteControlDropshipEntry>();
        var dropshipQuery = EntityQueryEnumerator<DropshipComponent>();
        while (dropshipQuery.MoveNext(out var uid, out var dropship))
        {
            if (!TryGetDropshipComputer(uid, out var computer))
                continue;

            controllableDropships.Add((uid, dropship, computer));
            TryComp(uid, out DropshipAutopilotComponent? autopilot);

            var location = dropship.Destination is { } destination && Exists(destination)
                ? Name(destination)
                : Loc.GetString("rmc-dropship-remote-location-unknown");

            if (TryComp(uid, out FTLComponent? ftl) &&
                ftl.State is FTLState.Starting or FTLState.Travelling or FTLState.Arriving)
            {
                location = Loc.GetString("rmc-dropship-remote-location-in-flight", ("location", location));
            }
            else if (ftl != null && ftl.State == FTLState.Cooldown)
            {
                location = Loc.GetString("rmc-dropship-remote-location-refueling", ("location", location));
            }

            NetEntity? routeHangar = null;
            if (autopilot != null && TryResolveRouteHangar((uid, dropship), autopilot, out var routeHangarUid))
            {
                routeHangar = GetNetEntity(routeHangarUid);
            }
            else if (TryResolveDestinationKind(dropship.Destination, DropshipDestinationKind.Hangar, out routeHangarUid))
            {
                routeHangar = GetNetEntity(routeHangarUid);
            }

            var mode = autopilot?.Mode ?? DropshipAutopilotMode.Disabled;
            var delay = autopilot?.Delay ?? _autopilotDefaultDelay;
            var lz = autopilot?.LandingZone is { } selectedLz && Exists(selectedLz)
                ? GetNetEntity(selectedLz)
                : (NetEntity?)null;

            dropships.Add(new DropshipRemoteControlDropshipEntry(
                GetNetEntity(computer.Owner),
                GetNetEntity(uid),
                Name(uid),
                location,
                routeHangar,
                lz,
                mode,
                (int)Math.Round(delay.TotalSeconds),
                autopilot == null ? null : GetAutopilotDepartInSeconds(autopilot),
                autopilot?.Status ?? DropshipAutopilotStatus.Offline,
                autopilot?.StatusDetails ?? string.Empty,
                HasComp<FTLComponent>(uid),
                dropship.Crashed));
        }

        var destinations = new List<DropshipRemoteControlDestinationEntry>();
        var hangars = new List<DropshipRemoteControlDestinationEntry>();
        var landingZones = new List<DropshipRemoteControlDestinationEntry>();
        var destinationQuery = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (destinationQuery.MoveNext(out var uid, out var destination))
        {
            var kind = ResolveDestinationKind((uid, destination));
            if (kind is not (DropshipDestinationKind.Hangar or DropshipDestinationKind.LandingZone))
                continue;

            var availableDropships = new List<NetEntity>();
            foreach (var candidate in controllableDropships)
            {
                if (CanUseDestinationForRemoteOrAutopilot(
                        candidate.Computer,
                        (candidate.Uid, candidate.Dropship),
                        (uid, destination),
                        kind,
                        false,
                        out _))
                {
                    availableDropships.Add(GetNetEntity(candidate.Uid));
                }
            }

            if (availableDropships.Count == 0)
                continue;

            var entry = new DropshipRemoteControlDestinationEntry(
                GetNetEntity(uid),
                Name(uid),
                destination.Ship is { } ship ? GetNetEntity(ship) : null,
                HasComp<PrimaryLandingZoneComponent>(uid),
                availableDropships);

            destinations.Add(entry);
            if (kind == DropshipDestinationKind.Hangar)
                hangars.Add(entry);
            else
                landingZones.Add(entry);
        }

        var linkedLandingZoneName = Loc.GetString("rmc-dropship-remote-linked-lz-none");
        if (TryResolveRemoteLinkedLandingZone(console, out var linked))
        {
            linkedLandingZoneName = Name(linked.Owner);
        }

        console.Comp.Planetside = IsRemoteControlConsolePlanetside(console);
        console.Comp.LinkedLandingZoneName = linkedLandingZoneName;
        console.Comp.Dropships = dropships;
        console.Comp.Destinations = destinations;
        console.Comp.Hangars = hangars;
        console.Comp.LandingZones = landingZones;
        console.Comp.DefaultDelaySeconds = (int) Math.Round(_autopilotDefaultDelay.TotalSeconds);
        console.Comp.MinDelaySeconds = (int) Math.Min(_autopilotMinDelay.TotalSeconds, _autopilotMaxDelay.TotalSeconds);
        console.Comp.MaxDelaySeconds = (int) Math.Max(_autopilotMinDelay.TotalSeconds, _autopilotMaxDelay.TotalSeconds);
        Dirty(console);
    }
}
