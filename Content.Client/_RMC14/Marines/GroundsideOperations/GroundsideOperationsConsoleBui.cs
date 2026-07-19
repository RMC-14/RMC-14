using Content.Client._RMC14.UserInterface.Crt;
using Content.Client.Message;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.GroundsideOperations;
using Content.Shared._RMC14.OrbitalCannon;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Marines.GroundsideOperations;

[UsedImplicitly]
public sealed class GroundsideOperationsConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private readonly IGameTiming _timing = IoCManager.Resolve<IGameTiming>();

    private GroundsideOperationsLandingZoneWindow? _landingZoneWindow;
    private GroundsideOperationsConsoleWindow? _window;
    private bool _confirmingEvacuation;
    private bool _confirmingGeneralQuarters;
    private bool _confirmingRedAlert;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<GroundsideOperationsConsoleWindow>();
        _window.OnTimeRefresh += RefreshTimeSensitive;
        _window.OpenOverwatchButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOverwatchMsg());
        _window.TacticalMapButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOpenMapMsg());
        _window.PrimaryLandingZoneButton.OnPressed += _ => OpenLandingZoneWindow();
        _window.EchoButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsEchoSquadMsg());
        _window.AlertButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerAlertLevelMsg());
        _window.ShipAnnouncementButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerShipAnnouncementMsg());
        _window.MedalsButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerOpenMedalsPanelMsg());
        _window.GroundAnnouncementButton.OnPressed += _ =>
            SendPredictedMessage(new MarineCommunicationsOpenAnnouncementMsg());
        _window.HighCommandButton.OnPressed += _ => SendPredictedMessage(new GroundsideOperationsHighCommandMsg());
        _window.RedAlertButton.OnPressed += _ => ConfirmRedAlert();
        _window.GeneralQuartersButton.OnPressed += _ => ConfirmGeneralQuarters();
        _window.EvacuationButton.OnPressed += _ => ConfirmEvacuation();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true } ||
            !EntMan.TryGetComponent(Owner, out GroundsideOperationsConsoleComponent? groundside))
        {
            return;
        }

        RefreshLandingZones(groundside);
        RefreshOrdnance(groundside);
        RefreshTimeSensitive();
    }

    private void RefreshLandingZones(GroundsideOperationsConsoleComponent groundside)
    {
        if (_window == null)
            return;

        if (groundside.PrimaryLandingZone is { } primaryLandingZone)
        {
            _window.PrimaryLandingZoneButton.Text = Loc.GetString(
                "rmc-goc-primary-lz-designated",
                ("landingZone", primaryLandingZone));
            _window.SetButtonState(_window.PrimaryLandingZoneButton, true, iconState: RMCCrtIcons.Home);
            _landingZoneWindow?.Close();
            return;
        }

        _window.PrimaryLandingZoneButton.Text = Loc.GetString("rmc-goc-primary-lz");
        _window.SetButtonState(
            _window.PrimaryLandingZoneButton,
            groundside.LandingZones.Count == 0,
            iconState: RMCCrtIcons.Home);

        if (_landingZoneWindow is { IsOpen: true })
            PopulateLandingZoneWindow(_landingZoneWindow, groundside.LandingZones);
    }

    private void OpenLandingZoneWindow()
    {
        if (!EntMan.TryGetComponent(Owner, out GroundsideOperationsConsoleComponent? groundside) ||
            groundside.PrimaryLandingZone != null ||
            groundside.LandingZones.Count == 0)
        {
            return;
        }

        _landingZoneWindow?.Close();
        var window = new GroundsideOperationsLandingZoneWindow();
        _landingZoneWindow = window;
        window.OnClose += () => _landingZoneWindow = null;
        PopulateLandingZoneWindow(window, groundside.LandingZones);
        window.OpenCentered();
    }

    private void PopulateLandingZoneWindow(
        GroundsideOperationsLandingZoneWindow window,
        IReadOnlyList<LandingZone> landingZones)
    {
        window.LandingZonesContainer.DisposeAllChildren();
        foreach (var landingZone in landingZones)
        {
            var button = new RMCCrtActionButton
            {
                Text = landingZone.Name,
                IconState = RMCCrtIcons.Home,
                Variant = RMCCrtButtonVariant.Filled,
                HorizontalExpand = true,
                MinHeight = 36,
            };
            button.OnPressed += _ =>
            {
                SendPredictedMessage(new MarineCommunicationsDesignatePrimaryLZMsg(landingZone.Id));
                window.Close();
            };
            window.LandingZonesContainer.AddChild(button);
        }
    }

    private void RefreshOrdnance(GroundsideOperationsConsoleComponent groundside)
    {
        if (_window == null)
            return;

        _window.OrbitalSubsystemStatus.SetMarkupPermissive(Loc.GetString(groundside.HasOrbitalCannon
            ? "rmc-goc-ordnance-operational"
            : "rmc-goc-ordnance-unavailable"));
        _window.WarheadStatus.SetMarkupPermissive(groundside.OrbitalWarhead is { } warhead
            ? Loc.GetString("rmc-goc-warhead-loaded", ("warhead", FormattedMessage.EscapeText(warhead)))
            : Loc.GetString("rmc-goc-warhead-empty"));
        _window.FuelStatus.SetMarkupPermissive(groundside.OrbitalRequiredFuel is { } requiredFuel
            ? Loc.GetString("rmc-goc-fuel-count-required", ("fuel", groundside.OrbitalFuel), ("required", requiredFuel))
            : Loc.GetString("rmc-goc-fuel-count", ("fuel", groundside.OrbitalFuel)));
        _window.OrbitalSafetyStatus.SetMarkupPermissive(Loc.GetString(groundside.OrbitalSafetyEngaged
            ? "rmc-goc-ob-safety-engaged"
            : "rmc-goc-ob-safety-disengaged"));
    }

    private void RefreshTimeSensitive()
    {
        if (_window is not { IsOpen: true } ||
            !EntMan.TryGetComponent(Owner, out GroundsideOperationsConsoleComponent? groundside))
        {
            return;
        }

        var alert = groundside.AlertLevel;
        _window.AlertStatus.Text = Loc.GetString($"rmc-goc-alert-status-{alert.ToString().ToLowerInvariant()}");

        var deltaLocked = alert == RMCAlertLevels.Delta;
        _window.SetButtonState(_window.AlertButton, deltaLocked);

        var redOrHigher = alert >= RMCAlertLevels.Red;
        if (redOrHigher)
            _confirmingRedAlert = false;
        _window.RedAlertButton.Text = redOrHigher
            ? Loc.GetString("rmc-goc-red-alert-already")
            : _confirmingRedAlert
                ? Loc.GetString("rmc-goc-confirm")
                : Loc.GetString("rmc-goc-red-alert");
        _window.SetButtonState(
            _window.RedAlertButton,
            redOrHigher,
            !redOrHigher,
            redOrHigher ? RMCCrtIcons.Ban : RMCCrtIcons.Warning);

        var generalQuartersLeft = groundside.NextGeneralQuarters - _timing.CurTime;
        var generalQuartersCooldown = generalQuartersLeft > TimeSpan.Zero;
        if (generalQuartersCooldown)
            _confirmingGeneralQuarters = false;

        _window.GeneralQuartersButton.Text = _confirmingGeneralQuarters
            ? Loc.GetString("rmc-goc-confirm-general-quarters")
            : Loc.GetString("rmc-goc-general-quarters");
        _window.GeneralQuartersStatus.Text = generalQuartersCooldown
            ? Loc.GetString("rmc-goc-general-quarters-countdown", ("seconds", Math.Max(1, (int) Math.Ceiling(generalQuartersLeft.TotalSeconds))))
            : string.Empty;
        _window.SetButtonState(
            _window.GeneralQuartersButton,
            generalQuartersCooldown,
            !generalQuartersCooldown,
            generalQuartersCooldown ? RMCCrtIcons.Ban : RMCCrtIcons.Warning);

        RefreshCommunicationsCooldowns();
        RefreshControlComputer();
        RefreshCannonCountdown(groundside);
    }

    private void RefreshCommunicationsCooldowns()
    {
        if (_window == null ||
            !EntMan.TryGetComponent(Owner, out MarineCommunicationsComputerComponent? communications) ||
            !EntMan.TryGetComponent(Owner, out GroundsideOperationsConsoleComponent? groundside))
        {
            return;
        }

        _window.SetButtonState(_window.EchoButton, !communications.CanCreateEcho);

        var groundAnnouncementLeft = Remaining(communications.LastAnnouncement, communications.Cooldown);
        SetCooldownButton(
            _window.GroundAnnouncementButton,
            "rmc-goc-groundside-announce",
            groundAnnouncementLeft);

        var highCommandLeft = Remaining(groundside.LastHighCommand, groundside.HighCommandCooldown);
        SetCooldownButton(_window.HighCommandButton, "rmc-goc-high-command", highCommandLeft);
    }

    private void RefreshControlComputer()
    {
        if (_window == null ||
            !EntMan.TryGetComponent(Owner, out MarineControlComputerComponent? control))
        {
            return;
        }

        var shipAnnouncementLeft = Remaining(control.LastShipAnnouncement, control.ShipAnnouncementCooldown);
        SetCooldownButton(_window.ShipAnnouncementButton, "rmc-goc-ship-announce", shipAnnouncementLeft);

        if (!control.CanEvacuate)
            _confirmingEvacuation = false;
        _window.EvacuationButton.Text = _confirmingEvacuation
            ? Loc.GetString("rmc-goc-confirm")
            : control.Evacuating
                ? Loc.GetString("rmc-goc-cancel-evacuation")
                : Loc.GetString("rmc-goc-initiate-evacuation");
        _window.SetButtonState(
            _window.EvacuationButton,
            !control.CanEvacuate,
            control.CanEvacuate,
            control.CanEvacuate ? RMCCrtIcons.DoorOpen : RMCCrtIcons.Ban);
    }

    private void RefreshCannonCountdown(GroundsideOperationsConsoleComponent groundside)
    {
        if (_window == null)
            return;

        if (!groundside.HasOrbitalCannon)
        {
            _window.CannonStatus.SetMarkupPermissive(Loc.GetString("rmc-goc-cannon-unavailable"));
            return;
        }

        var cooldown = groundside.NextOrbitalFire - _timing.CurTime;
        if (cooldown > TimeSpan.Zero)
        {
            _window.CannonStatus.SetMarkupPermissive(Loc.GetString(
                "rmc-goc-cannon-cooldown",
                ("seconds", Math.Max(1, (int) Math.Ceiling(cooldown.TotalSeconds)))));
            return;
        }

        var status = groundside.OrbitalStatus switch
        {
            OrbitalCannonStatus.Unloaded => "rmc-goc-cannon-unloaded",
            OrbitalCannonStatus.Loaded => "rmc-goc-cannon-loaded",
            OrbitalCannonStatus.Chambered => "rmc-goc-cannon-chambered",
            _ => "rmc-goc-cannon-unavailable",
        };
        _window.CannonStatus.SetMarkupPermissive(Loc.GetString(status));
    }

    private TimeSpan Remaining(TimeSpan? lastUsed, TimeSpan cooldown)
    {
        if (lastUsed == null)
            return TimeSpan.Zero;

        return lastUsed.Value + cooldown - _timing.CurTime;
    }

    private void SetCooldownButton(
        RMCCrtActionButton button,
        string text,
        TimeSpan remaining)
    {
        if (_window == null)
            return;

        var disabled = remaining > TimeSpan.Zero;
        button.Text = disabled
            ? Loc.GetString(
                "rmc-goc-button-cooldown",
                ("action", Loc.GetString(text)),
                ("seconds", Math.Max(1, (int) Math.Ceiling(remaining.TotalSeconds))))
            : Loc.GetString(text);
        _window.SetButtonState(button, disabled);
    }

    private void ConfirmEvacuation()
    {
        if (_confirmingEvacuation)
            SendPredictedMessage(new MarineControlComputerToggleEvacuationMsg());

        _confirmingEvacuation = !_confirmingEvacuation;
        RefreshTimeSensitive();
    }

    private void ConfirmGeneralQuarters()
    {
        if (_confirmingGeneralQuarters)
            SendPredictedMessage(new GroundsideOperationsGeneralQuartersMsg());

        _confirmingGeneralQuarters = !_confirmingGeneralQuarters;
        RefreshTimeSensitive();
    }

    private void ConfirmRedAlert()
    {
        if (_confirmingRedAlert)
            SendPredictedMessage(new GroundsideOperationsRedAlertMsg());

        _confirmingRedAlert = !_confirmingRedAlert;
        RefreshTimeSensitive();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _landingZoneWindow?.Close();
    }
}
