using System.Linq;
using Content.Client.Message;
using Content.Shared._RMC14.Dropship;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Dropship;

[UsedImplicitly]
public sealed class DropshipRemoteControlBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [ViewVariables]
    private DropshipRemoteControlWindow? _window;

    private readonly Dictionary<DropshipButton, string> _dropships = new();
    private readonly Dictionary<DropshipButton, string> _destinations = new();
    private readonly Dictionary<DropshipButton, string> _hangars = new();
    private readonly Dictionary<DropshipButton, string> _landingZones = new();

    private DropshipRemoteControlBuiState? _state;
    private NetEntity? _selectedComputer;
    private NetEntity? _selectedDestination;
    private NetEntity? _selectedHangar;
    private NetEntity? _selectedLandingZone;
    private DropshipAutopilotMode _selectedMode = DropshipAutopilotMode.Cycle;
    private int _delaySeconds;

    public DropshipRemoteControlBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        OpenWindow();

        if (State is DropshipRemoteControlBuiState s)
            Set(s);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        OpenWindow();

        if (state is DropshipRemoteControlBuiState s)
            Set(s);
    }

    private void OpenWindow()
    {
        if (_window != null)
            return;

        _window = this.CreateWindow<DropshipRemoteControlWindow>();
        _window.OnClose += OnClose;
        SetHeader(_window.Header, Loc.GetString("rmc-dropship-remote-ui-title"));
        SetHeader(_window.DropshipsHeader, Loc.GetString("rmc-dropship-remote-ui-dropships"));
        SetHeader(_window.StatusHeader, Loc.GetString("rmc-dropship-remote-ui-flight-status"));
        SetHeader(_window.DestinationsHeader, Loc.GetString("rmc-dropship-remote-ui-destinations"));
        SetHeader(_window.AutopilotHeader, Loc.GetString("rmc-dropship-remote-ui-autopilot"));
        SetFieldLabel(_window.ModeHeader, Loc.GetString("rmc-dropship-remote-ui-mode"));
        SetFieldLabel(_window.HangarsHeader, Loc.GetString("rmc-dropship-remote-ui-hangar"));
        SetFieldLabel(_window.LandingZonesHeader, Loc.GetString("rmc-dropship-remote-ui-landing-zones"));
        SetFieldLabel(_window.DelayHeader, Loc.GetString("rmc-dropship-remote-ui-delay"));
        _window.ManualLaunchButton.Text = Loc.GetString("rmc-dropship-remote-ui-launch-selected");
        _window.EnableButton.Text = Loc.GetString("rmc-dropship-remote-ui-enable");
        _window.LaunchNowButton.Text = Loc.GetString("rmc-dropship-remote-ui-autopilot-launch");
        _window.RecallNowButton.Text = Loc.GetString("rmc-dropship-remote-ui-recall-now");

        _window.CycleButton.Button.OnPressed += _ =>
        {
            _selectedMode = DropshipAutopilotMode.Cycle;
            RefreshModeButtons();
        };

        _window.RecallOnlyButton.Button.OnPressed += _ =>
        {
            _selectedMode = DropshipAutopilotMode.RecallOnly;
            RefreshModeButtons();
        };

        _window.ManualLaunchButton.Button.OnPressed += _ => LaunchSelected();
        _window.EnableButton.Button.OnPressed += _ => ToggleAutopilot();
        _window.LaunchNowButton.Button.OnPressed += _ => LaunchAutopilotNow();
        _window.RecallNowButton.Button.OnPressed += _ => RecallAutopilotNow();

        _entities.System<DropshipSystem>().RemoteControlUis.Add(this);
    }

    private void OnClose()
    {
        _entities.System<DropshipSystem>().RemoteControlUis.Remove(this);
        Close();
    }

    private void Set(DropshipRemoteControlBuiState state)
    {
        if (_window == null)
            return;

        _state = state;
        _delaySeconds = _delaySeconds == 0 ? state.DefaultDelaySeconds : _delaySeconds;
        _window.Title = state.Kind == DropshipRemoteConsoleKind.Planetside
            ? Loc.GetString("rmc-dropship-remote-ui-title-planetside")
            : Loc.GetString("rmc-dropship-remote-ui-title");

        if (_selectedComputer == null || state.Dropships.All(d => d.Computer != _selectedComputer))
        {
            if (state.Dropships.Count > 0)
                SelectDropship(state.Dropships[0]);
            else
                _selectedComputer = null;
        }

        NormalizeSelections(state);

        FillDropships(state);
        FillDestinations(state);
        FillHangars(state);
        FillLandingZones(state);
        RefreshSelectedDetails();
        RefreshModeButtons();
        RefreshActionButtons();
    }

    private void SelectDropship(DropshipRemoteControlDropshipEntry entry)
    {
        _selectedComputer = entry.Computer;
        _selectedMode = entry.Mode == DropshipAutopilotMode.Disabled
            ? DropshipAutopilotMode.Cycle
            : entry.Mode;
        _selectedHangar = entry.RouteHangar;
        _selectedLandingZone = entry.LandingZone;
        _delaySeconds = entry.DelaySeconds;
    }

    private void NormalizeSelections(DropshipRemoteControlBuiState state)
    {
        var selected = state.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        if (selected.Computer == default)
        {
            _selectedDestination = null;
            _selectedHangar = null;
            _selectedLandingZone = null;
            return;
        }

        var destinations = DestinationsForDropship(state.Destinations, selected.Dropship).ToList();
        if (_selectedDestination == null || destinations.All(d => d.Id != _selectedDestination))
        {
            if (state.LinkedLandingZone is { } linked && destinations.Any(d => d.Id == linked))
                _selectedDestination = linked;
            else if (destinations.Count > 0)
                _selectedDestination = destinations[0].Id;
            else
                _selectedDestination = null;
        }

        var hangars = DestinationsForDropship(state.Hangars, selected.Dropship).ToList();
        if (_selectedHangar == null || hangars.All(h => h.Id != _selectedHangar))
        {
            if (selected.RouteHangar is { } routeHangar && hangars.Any(h => h.Id == routeHangar))
                _selectedHangar = routeHangar;
            else if (hangars.Count > 0)
                _selectedHangar = hangars[0].Id;
            else
                _selectedHangar = null;
        }

        var landingZones = DestinationsForDropship(state.LandingZones, selected.Dropship).ToList();
        if (_selectedLandingZone == null || landingZones.All(lz => lz.Id != _selectedLandingZone))
        {
            if (selected.LandingZone is { } lz && landingZones.Any(entry => entry.Id == lz))
                _selectedLandingZone = lz;
            else if (state.LinkedLandingZone is { } linked && landingZones.Any(entry => entry.Id == linked))
                _selectedLandingZone = linked;
            else
            {
                var primary = landingZones.FirstOrDefault(lzEntry => lzEntry.Primary);
                if (primary.Id != default)
                    _selectedLandingZone = primary.Id;
                else if (landingZones.Count > 0)
                    _selectedLandingZone = landingZones[0].Id;
                else
                    _selectedLandingZone = null;
            }
        }
    }

    private void FillDropships(DropshipRemoteControlBuiState state)
    {
        if (_window == null)
            return;

        _dropships.Clear();
        _window.DropshipsContainer.DisposeAllChildren();

        foreach (var dropship in state.Dropships)
        {
            var name = dropship.Name;
            var label = _selectedComputer == dropship.Computer ? $"> {name}" : name;
            var button = Row(label, false, () =>
            {
                SelectDropship(dropship);
                NormalizeSelections(state);
                FillDropships(state);
                FillDestinations(state);
                FillHangars(state);
                FillLandingZones(state);
                RefreshSelectedDetails();
                RefreshModeButtons();
                RefreshActionButtons();
            });

            _dropships[button] = name;
            _window.DropshipsContainer.AddChild(button);
        }
    }

    private void FillDestinations(DropshipRemoteControlBuiState state)
    {
        if (_window == null)
            return;

        _destinations.Clear();
        _window.DestinationsContainer.DisposeAllChildren();

        var selectedDropship = state.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        foreach (var destination in DestinationsForDropship(state.Destinations, selectedDropship.Dropship))
        {
            var name = DestinationName(destination, selectedDropship.Dropship);
            var label = _selectedDestination == destination.Id ? $"> {name}" : name;
            var disabled = IsOccupiedByOther(destination, selectedDropship.Dropship);
            var button = Row(label, disabled, () =>
            {
                _selectedDestination = destination.Id;
                FillDestinations(state);
                RefreshActionButtons();
            });

            _destinations[button] = name;
            _window.DestinationsContainer.AddChild(button);
        }
    }

    private void FillHangars(DropshipRemoteControlBuiState state)
    {
        if (_window == null)
            return;

        _hangars.Clear();
        _window.HangarsContainer.DisposeAllChildren();

        var selectedDropship = state.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        foreach (var hangar in DestinationsForDropship(state.Hangars, selectedDropship.Dropship))
        {
            var name = DestinationName(hangar, selectedDropship.Dropship);
            var label = _selectedHangar == hangar.Id ? $"> {name}" : name;
            var disabled = IsOccupiedByOther(hangar, selectedDropship.Dropship);
            var button = Row(label, disabled, () =>
            {
                _selectedHangar = hangar.Id;
                FillHangars(state);
                RefreshSelectedDetails();
                RefreshActionButtons();
            });

            _hangars[button] = name;
            _window.HangarsContainer.AddChild(button);
        }
    }

    private void FillLandingZones(DropshipRemoteControlBuiState state)
    {
        if (_window == null)
            return;

        _landingZones.Clear();
        _window.LandingZonesContainer.DisposeAllChildren();

        var selectedDropship = state.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        foreach (var lz in DestinationsForDropship(state.LandingZones, selectedDropship.Dropship))
        {
            var name = DestinationName(lz, selectedDropship.Dropship);
            var label = _selectedLandingZone == lz.Id ? $"> {name}" : name;
            var disabled = IsOccupiedByOther(lz, selectedDropship.Dropship);
            var button = Row(label, disabled, () =>
            {
                _selectedLandingZone = lz.Id;
                FillLandingZones(state);
                RefreshSelectedDetails();
                RefreshActionButtons();
            });

            _landingZones[button] = name;
            _window.LandingZonesContainer.AddChild(button);
        }
    }

    private DropshipButton Row(string label, bool disabled, Action onPressed)
    {
        var button = new DropshipButton
        {
            Text = label,
            Disabled = disabled,
            BorderColor = Color.Transparent,
            BorderThickness = new Thickness(0)
        };

        button.Button.ToggleMode = false;
        button.Button.OnPressed += _ => onPressed();
        return button;
    }

    private void LaunchSelected()
    {
        if (_selectedComputer is { } computer && _selectedDestination is { } destination)
            SendPredictedMessage(new DropshipRemoteLaunchMsg(computer, destination));
    }

    private void ConfigureAutopilot()
    {
        if (_selectedComputer is not { } computer ||
            _selectedHangar is not { } hangar ||
            _selectedLandingZone is not { } landingZone ||
            _state == null)
        {
            return;
        }

        var delay = ParseDelay();
        SendPredictedMessage(new DropshipRemoteAutopilotConfigureMsg(computer, _selectedMode, hangar, landingZone, delay));
    }

    private void ToggleAutopilot()
    {
        if (_state == null)
            return;

        var selected = _state.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        if (selected.Computer != default && selected.Mode != DropshipAutopilotMode.Disabled)
        {
            DisableAutopilot();
            return;
        }

        ConfigureAutopilot();
    }

    private void DisableAutopilot()
    {
        if (_selectedComputer is { } computer)
            SendPredictedMessage(new DropshipRemoteAutopilotDisableMsg(computer));
    }

    private void LaunchAutopilotNow()
    {
        if (_selectedComputer is { } computer)
            SendPredictedMessage(new DropshipRemoteAutopilotLaunchNowMsg(computer));
    }

    private void RecallAutopilotNow()
    {
        if (_selectedComputer is { } computer)
            SendPredictedMessage(new DropshipRemoteAutopilotRecallNowMsg(computer));
    }

    private int ParseDelay()
    {
        if (_state == null)
            return _delaySeconds;

        if (_window != null && int.TryParse(_window.DelayEdit.Text, out var parsed))
            _delaySeconds = parsed;

        _delaySeconds = Math.Clamp(_delaySeconds, _state.MinDelaySeconds, _state.MaxDelaySeconds);
        return _delaySeconds;
    }

    private void RefreshSelectedDetails()
    {
        if (_window == null || _state == null)
            return;

        var selected = _state.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        if (selected.Computer == default)
        {
            _window.StatusLabel.SetMarkup($"[color=#02E74E]{Loc.GetString("rmc-dropship-remote-ui-no-dropships")}[/color]");
            _window.DelayEdit.Text = _state.DefaultDelaySeconds.ToString();
            return;
        }

        var statusName = StatusName(selected.Status);
        var details = string.IsNullOrWhiteSpace(selected.StatusDetails)
            ? string.Empty
            : selected.StatusDetails.Trim();

        if (selected.DepartInSeconds is { } departIn)
            details = string.IsNullOrWhiteSpace(details)
                ? $"T-{departIn}s"
                : $"{details} T-{departIn}s";

        var linked = _state.Kind == DropshipRemoteConsoleKind.Planetside
            ? "\n" + Loc.GetString("rmc-dropship-remote-ui-linked-lz", ("lz", _state.LinkedLandingZoneName))
            : string.Empty;

        var autopilotStatus = string.IsNullOrWhiteSpace(details) || details == statusName
            ? Loc.GetString("rmc-dropship-remote-ui-autopilot-status-simple", ("status", statusName))
            : Loc.GetString("rmc-dropship-remote-ui-autopilot-status",
                ("status", statusName),
                ("details", details));

        _window.StatusLabel.SetMarkup(
            $"[color=#02E74E][bold]{selected.Name}[/bold][/color]\n" +
            Loc.GetString("rmc-dropship-remote-ui-location", ("location", selected.Location)) + "\n" +
            Loc.GetString("rmc-dropship-remote-ui-from", ("hangar", selected.RouteHangarName)) + linked + "\n" +
            autopilotStatus);

        if (!_window.DelayEdit.HasKeyboardFocus())
            _window.DelayEdit.Text = _delaySeconds.ToString();
    }

    private void RefreshModeButtons()
    {
        if (_window == null)
            return;

        var cycle = Loc.GetString("rmc-dropship-remote-ui-cycle");
        var recallOnly = Loc.GetString("rmc-dropship-remote-ui-recall-only");
        _window.CycleButton.Text = _selectedMode == DropshipAutopilotMode.Cycle ? $"> {cycle}" : cycle;
        _window.RecallOnlyButton.Text = _selectedMode == DropshipAutopilotMode.RecallOnly ? $"> {recallOnly}" : recallOnly;
    }

    private void RefreshActionButtons()
    {
        if (_window == null || _state == null)
            return;

        var selected = _state.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        var hasDropship = selected.Computer != default;
        var active = hasDropship && selected.Mode != DropshipAutopilotMode.Disabled;
        var busy = selected.Crashed || selected.InFlight;
        var destination = _state.Destinations.FirstOrDefault(d => d.Id == _selectedDestination);
        var hangar = _state.Hangars.FirstOrDefault(d => d.Id == _selectedHangar);
        var landingZone = _state.LandingZones.FirstOrDefault(d => d.Id == _selectedLandingZone);
        var canUseDestination = IsSelectableDestination(destination, selected.Dropship) &&
                                !IsOccupiedByOther(destination, selected.Dropship) &&
                                !IsCurrentDestination(destination, selected.Dropship);
        var canUseHangar = IsSelectableDestination(hangar, selected.Dropship) &&
                           !IsOccupiedByOther(hangar, selected.Dropship);
        var canUseLandingZone = IsSelectableDestination(landingZone, selected.Dropship) &&
                                !IsOccupiedByOther(landingZone, selected.Dropship);

        _window.ManualLaunchButton.Disabled = !hasDropship || busy || !canUseDestination;
        _window.EnableButton.Text = active
            ? Loc.GetString("rmc-dropship-remote-ui-disable")
            : Loc.GetString("rmc-dropship-remote-ui-enable");
        _window.EnableButton.Disabled = active
            ? !hasDropship
            : !hasDropship || !canUseHangar || !canUseLandingZone;
        _window.LaunchNowButton.Disabled = !active || busy;
        _window.RecallNowButton.Disabled = !active || busy;
    }

    private static IEnumerable<DropshipRemoteControlDestinationEntry> DestinationsForDropship(
        IEnumerable<DropshipRemoteControlDestinationEntry> destinations,
        NetEntity selectedDropship)
    {
        if (selectedDropship == default)
            return Enumerable.Empty<DropshipRemoteControlDestinationEntry>();

        return destinations.Where(destination => IsSelectableDestination(destination, selectedDropship));
    }

    private static bool IsSelectableDestination(DropshipRemoteControlDestinationEntry destination, NetEntity selectedDropship)
    {
        return destination.Id != default &&
               selectedDropship != default &&
               destination.AvailableDropships.Contains(selectedDropship);
    }

    private static string DestinationName(DropshipRemoteControlDestinationEntry destination, NetEntity selectedDropship)
    {
        var name = destination.Name;
        if (destination.Primary)
            name += Loc.GetString("rmc-dropship-remote-ui-primary");

        if (destination.OccupiedBy is { } occupiedBy)
        {
            name += occupiedBy == selectedDropship
                ? Loc.GetString("rmc-dropship-remote-ui-current")
                : Loc.GetString("rmc-dropship-remote-ui-occupied");
        }

        return name;
    }

    private static bool IsOccupiedByOther(DropshipRemoteControlDestinationEntry destination, NetEntity selectedDropship)
    {
        return destination.OccupiedBy is { } occupiedBy && occupiedBy != selectedDropship;
    }

    private static bool IsCurrentDestination(DropshipRemoteControlDestinationEntry destination, NetEntity selectedDropship)
    {
        return destination.OccupiedBy is { } occupiedBy && occupiedBy == selectedDropship;
    }

    private void SetHeader(RichTextLabel label, string text)
    {
        label.SetMarkup($"[color=#0BDC49][font size=16][bold]{text}[/bold][/font][/color]");
    }

    private void SetFieldLabel(RichTextLabel label, string text)
    {
        label.SetMarkup($"[color=#0BDC49][font size=12][bold]{text}[/bold][/font][/color]");
    }

    private static string StatusName(DropshipAutopilotStatus status)
    {
        return status switch
        {
            DropshipAutopilotStatus.Ready => Loc.GetString("rmc-dropship-autopilot-status-ready"),
            DropshipAutopilotStatus.Waiting => Loc.GetString("rmc-dropship-autopilot-status-waiting"),
            DropshipAutopilotStatus.InFlight => Loc.GetString("rmc-dropship-autopilot-status-in-flight"),
            DropshipAutopilotStatus.Blocked => Loc.GetString("rmc-dropship-autopilot-status-blocked"),
            DropshipAutopilotStatus.Error => Loc.GetString("rmc-dropship-autopilot-status-error"),
            _ => Loc.GetString("rmc-dropship-autopilot-status-offline"),
        };
    }

    public new void Update()
    {
        if (_window == null || _window.Disposed)
            return;

        RefreshSelectedDetails();
    }
}
