using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Xenonids;

namespace Content.Server._RMC14.Dropship;

public sealed partial class DropshipSystem
{
    private void OnRemoteControlActivateInWorld(Entity<DropshipRemoteControlConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        var user = args.User;
        if (!HasComp<XenoComponent>(user))
            return;

        args.Handled = true;

        if (!IsRemoteControlConsolePlanetside(ent))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-xeno-clueless", ("console", ent.Owner)), user, user);
            return;
        }

        if (!HasComp<DropshipHijackerComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-xeno-clueless", ("console", ent.Owner)), user, user);
            return;
        }

        if (!TryDropshipLaunchPopup(ent, user, false))
            return;

        if (!TryDropshipHijackPopup(ent, user, false))
            return;

        if (!TryResolveRemoteLinkedLandingZone(ent, out var landingZone))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-no-lz"), user, user, PopupType.MediumCaution);
            return;
        }

        if (landingZone.Comp.Ship != null)
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-destination-occupied"), user, user, PopupType.MediumCaution);
            return;
        }

        if (Count<PrimaryLandingZoneComponent>() > 0 &&
            !HasComp<PrimaryLandingZoneComponent>(landingZone))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-not-primary"), user, user, PopupType.MediumCaution);
            return;
        }

        if (TrySummonAnyAvailableDropship(user, landingZone))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-xeno-called"), user, user, PopupType.LargeCaution);

            var locationName = Loc.GetString("rmc-dropship-remote-location-unknown");
            if (_area.TryGetArea(landingZone, out _, out var areaProto))
                locationName = areaProto.Name;

            _xenoAnnounce.AnnounceSameHiveDefaultSound(user, Loc.GetString("rmc-dropship-remote-xeno-announce", ("location", locationName)));
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-no-dropships"), user, user, PopupType.LargeCaution);
    }

    private void OnRemoteControlOpenAttempt(Entity<DropshipRemoteControlConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<XenoComponent>(args.User))
            args.Cancel();
    }

    private void OnRemoteControlConsoleOpen(Entity<DropshipRemoteControlConsoleComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (!_ui.IsUiOpen(ent.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        RefreshRemoteControlConsole(ent);
    }

    private void OnDropshipNavigationAutopilotDisableMsg(Entity<DropshipNavigationComputerComponent> ent, ref DropshipNavigationAutopilotDisableMsg args)
    {
        if (!TryGetGridDropship(ent, out var dropship) ||
            !TryComp(dropship, out DropshipAutopilotComponent? autopilot))
        {
            return;
        }

        SetAutopilotDisabled(dropship, autopilot, args.Actor, Loc.GetString("rmc-dropship-autopilot-status-detail-disabled-flight-computer"));
        RefreshUI();
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteLaunchMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteLaunchMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to remotely pilot an invalid dropship computer {args.Computer}");
            return;
        }

        if (!TryGetEntity(args.Destination, out var destination) ||
            !TryComp(destination, out DropshipDestinationComponent? destinationComp))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-invalid-destination"), console, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!CanRemoteLaunchTo(computer, dropship, (destination.Value, destinationComp), out var reason))
        {
            _popup.PopupEntity(reason, console, args.Actor, PopupType.MediumCaution);
            RefreshRemoteControlConsoles();
            return;
        }

        if (!FlyTo(computer, destination.Value, args.Actor, source: DropshipLaunchSource.RemoteNavigation))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-error-busy"), console, args.Actor, PopupType.MediumCaution);
            RefreshRemoteControlConsoles();
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-dropship-remote-launched", ("destination", Name(destination.Value))), console, args.Actor, PopupType.Medium);
        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotConfigureMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotConfigureMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship))
        {
            Log.Warning($"{ToPrettyString(args.Actor)} tried to configure autopilot for invalid dropship computer {args.Computer}");
            return;
        }

        if (args.Mode is not (DropshipAutopilotMode.Disabled or DropshipAutopilotMode.Cycle or DropshipAutopilotMode.RecallOnly))
            return;

        var autopilot = EnsureComp<DropshipAutopilotComponent>(dropship);

        if (args.Mode == DropshipAutopilotMode.Disabled)
        {
            SetAutopilotDisabled(dropship, autopilot, args.Actor, Loc.GetString("rmc-dropship-autopilot-status-detail-disabled-remote"));
            RefreshUI(computer);
            RefreshRemoteControlConsoles();
            return;
        }

        if (_autopilotHijackStarted)
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-status-detail-hijack-disabled"),
                console, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (args.RouteHangar == null ||
            !TryGetEntity(args.RouteHangar.Value, out var routeHangar) ||
            !TryComp(routeHangar, out DropshipDestinationComponent? routeHangarDestination) ||
            !CanUseDestinationForRemoteOrAutopilot(
                computer,
                dropship,
                (routeHangar.Value, routeHangarDestination),
                DropshipDestinationKind.Hangar,
                false,
                out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-error-no-home"), console, args.Actor, PopupType.MediumCaution);
            RefreshUI(computer);
            RefreshRemoteControlConsoles();
            return;
        }

        if (IsDestinationOccupiedByOther(dropship, routeHangarDestination))
        {
            _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-error-hangar-occupied"), console, args.Actor, PopupType.MediumCaution);
            RefreshRemoteControlConsoles();
            return;
        }

        EntityUid? lz = null;
        if (args.Mode == DropshipAutopilotMode.Cycle)
        {
            if (args.LandingZone is not { } landingZone ||
                !TryGetEntity(landingZone, out lz) ||
                !TryComp(lz, out DropshipDestinationComponent? lzDestination) ||
                !CanUseDestinationForRemoteOrAutopilot(computer, dropship,
                    (lz.Value, lzDestination), DropshipDestinationKind.LandingZone, false, out _) ||
                IsDestinationOccupiedByOther(dropship, lzDestination))
            {
                _popup.PopupEntity(Loc.GetString("rmc-dropship-autopilot-error-invalid-lz"),
                    console, args.Actor, PopupType.MediumCaution);
                return;
            }
        }

        autopilot.RouteHangar = routeHangar.Value;
        autopilot.LandingZone = lz;
        autopilot.Delay = ClampAutopilotDelay(TimeSpan.FromSeconds(args.DelaySeconds));
        autopilot.Mode = args.Mode;
        autopilot.NextDepartureAt = null;
        autopilot.RetryAt = null;
        SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Ready, Loc.GetString("rmc-dropship-autopilot-status-detail-route-armed"));
        Dirty(dropship.Owner, autopilot);

        _adminLog.Add(LogType.RMCDropshipAutopilot,
            $"{ToPrettyString(args.Actor):player} configured autopilot on {ToPrettyString(dropship):dropship} for {args.Mode} between {ToPrettyString(routeHangar):hangar} and {ToPrettyString(lz):landingzone}");

        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotDisableMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotDisableMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship))
            return;

        if (TryComp(dropship, out DropshipAutopilotComponent? autopilot))
            SetAutopilotDisabled(dropship, autopilot, args.Actor, Loc.GetString("rmc-dropship-autopilot-status-detail-disabled-remote"));

        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotLaunchNowMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotLaunchNowMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship) ||
            !TryComp(dropship, out DropshipAutopilotComponent? autopilot) ||
            autopilot.Mode == DropshipAutopilotMode.Disabled)
        {
            return;
        }

        if (!TryGetNextAutopilotTarget(dropship, autopilot, out var target, out var reason))
        {
            var holdingAtRouteHangar = Loc.GetString("rmc-dropship-autopilot-status-detail-holding-hangar");
            if (reason == holdingAtRouteHangar)
            {
                autopilot.NextDepartureAt = null;
                autopilot.RetryAt = null;
                Dirty(dropship.Owner, autopilot);
                SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Ready, reason);
            }
            else
            {
                SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Error, reason);
            }

            RefreshRemoteControlConsoles();
            return;
        }

        LaunchAutopilotNow(dropship, autopilot, computer, target);
        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }

    private void OnDropshipRemoteAutopilotRecallNowMsg(Entity<DropshipRemoteControlConsoleComponent> console, ref DropshipRemoteAutopilotRecallNowMsg args)
    {
        if (!_ui.IsUiOpen(console.Owner, DropshipRemoteControlUiKey.Key, args.Actor))
            return;

        if (!TryGetAutopilotDropship(args.Computer, out var computer, out var dropship) ||
            !TryComp(dropship, out DropshipAutopilotComponent? autopilot) ||
            autopilot.Mode == DropshipAutopilotMode.Disabled)
        {
            return;
        }

        if (!TryResolveRouteHangar(dropship, autopilot, out var routeHangar))
        {
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Error, Loc.GetString("rmc-dropship-autopilot-status-detail-select-hangar"));
            RefreshRemoteControlConsoles();
            return;
        }

        if (dropship.Comp.Destination == routeHangar)
        {
            autopilot.NextDepartureAt = null;
            autopilot.RetryAt = null;
            SetAutopilotStatus(dropship, autopilot, DropshipAutopilotStatus.Ready, Loc.GetString("rmc-dropship-autopilot-status-detail-holding-hangar"));
            Dirty(dropship.Owner, autopilot);
        }
        else
        {
            LaunchAutopilotNow(dropship, autopilot, computer, routeHangar);
        }

        RefreshUI(computer);
        RefreshRemoteControlConsoles();
    }
}
