using System.Linq;
using Content.Client._RMC14.UserInterface.Crt;
using Content.Shared._RMC14.Dropship;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Dropship;

[UsedImplicitly]
public sealed class DropshipRemoteControlBui : BoundUserInterface
{
    private DropshipRemoteControlWindow? _window;
    private NetEntity? _selectedComputer;
    private NetEntity? _selectedDestination;
    private NetEntity? _selectedHangar;
    private NetEntity? _selectedLandingZone;
    private DropshipAutopilotMode _selectedMode = DropshipAutopilotMode.Cycle;
    private readonly Dictionary<RMCCrtOptionButton, List<(NetEntity Id, string Label)>> _options = new();

    public DropshipRemoteControlBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _options.Clear();
        _selectedComputer = null;
        _window = this.CreateWindow<DropshipRemoteControlWindow>();
        _window.Dropships.OnItemSelected += id =>
        {
            if (!EntMan.TryGetComponent(Owner, out DropshipRemoteControlConsoleComponent? console) ||
                id < 0 || id >= console.Dropships.Count)
            {
                return;
            }

            SelectDropship(console.Dropships[id]);
            Refresh();
        };
        _window.Destinations.OnItemSelected += id =>
            SelectDestination(_window.Destinations, id, ref _selectedDestination);
        _window.Hangars.OnItemSelected += id => SelectDestination(_window.Hangars, id, ref _selectedHangar);
        _window.LandingZones.OnItemSelected += id =>
            SelectDestination(_window.LandingZones, id, ref _selectedLandingZone);
        _window.CycleButton.OnPressed += _ => SelectMode(DropshipAutopilotMode.Cycle);
        _window.RecallOnlyButton.OnPressed += _ => SelectMode(DropshipAutopilotMode.RecallOnly);
        _window.ManualLaunchButton.OnPressed += _ =>
        {
            if (_selectedComputer is { } computer && _selectedDestination is { } destination)
                SendPredictedMessage(new DropshipRemoteLaunchMsg(computer, destination));
        };
        _window.ApplyButton.OnPressed += _ => ConfigureAutopilot();
        _window.DisableButton.OnPressed += _ =>
        {
            if (_selectedComputer is { } computer)
                SendPredictedMessage(new DropshipRemoteAutopilotDisableMsg(computer));
        };
        _window.LaunchNowButton.OnPressed += _ =>
        {
            if (_selectedComputer is { } computer)
                SendPredictedMessage(new DropshipRemoteAutopilotLaunchNowMsg(computer));
        };
        _window.RecallNowButton.OnPressed += _ =>
        {
            if (_selectedComputer is { } computer)
                SendPredictedMessage(new DropshipRemoteAutopilotRecallNowMsg(computer));
        };
        Refresh();
    }

    public void Refresh()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out DropshipRemoteControlConsoleComponent? console))
            return;

        _window.Title = Loc.GetString(console.Planetside
            ? "rmc-dropship-remote-ui-title-planetside"
            : "rmc-dropship-remote-ui-title");
        _window.LinkedLandingZone.Visible = console.Planetside;
        _window.LinkedLandingZone.Text = Loc.GetString("rmc-dropship-remote-ui-linked-lz",
            ("lz", console.LinkedLandingZoneName));

        var selected = console.Dropships.FirstOrDefault(d => d.Computer == _selectedComputer);
        if (selected.Computer == default && console.Dropships.Count > 0)
        {
            selected = console.Dropships[0];
            SelectDropship(selected);
        }

        _selectedComputer = selected.Computer == default ? null : selected.Computer;
        if (_selectedComputer == null)
        {
            var defaultDelay = console.DefaultDelaySeconds;
            _window.DelayEdit.Text = defaultDelay.ToString();
        }
        FillOptions(_window.Dropships, console.Dropships.Select(d => (d.Computer, d.Name)).ToList(), _selectedComputer);
        _window.Dropships.Disabled = console.Dropships.Count == 0;
        _window.Location.Text = selected.Computer == default
            ? Loc.GetString("rmc-dropship-remote-ui-no-dropships")
            : Loc.GetString("rmc-dropship-remote-ui-location", ("location", selected.Location));
        _window.Status.Visible = selected.Computer != default;
        _window.Status.Text = selected.Mode == DropshipAutopilotMode.Disabled
            ? Loc.GetString("rmc-dropship-autopilot-status",
                ("status", DropshipAutopilotText.Status(selected.Status)))
            : Loc.GetString("rmc-dropship-flight-status",
                ("mode", DropshipAutopilotText.Mode(selected.Mode)),
                ("status", DropshipAutopilotText.Status(selected.Status)));
        _window.Status.Tone = DropshipAutopilotText.Tone(selected.Status);
        _window.StatusDetails.Text = DropshipAutopilotText.Details(selected.StatusDetails, selected.DepartInSeconds);
        _window.StatusDetails.Visible = !string.IsNullOrWhiteSpace(_window.StatusDetails.Text);

        _selectedDestination = FillDestinations(_window.Destinations, console.Destinations,
            selected.Dropship, _selectedDestination,
            console.LinkedLandingZone is { } linked ? EntMan.GetNetEntity(linked) : null);
        _selectedHangar = FillDestinations(_window.Hangars, console.Hangars,
            selected.Dropship, _selectedHangar, selected.RouteHangar);
        _selectedLandingZone = FillDestinations(_window.LandingZones, console.LandingZones,
            selected.Dropship, _selectedLandingZone, selected.LandingZone ??
            (console.LinkedLandingZone is { } lz ? EntMan.GetNetEntity(lz) : null));

        _window.CycleButton.Selected = _selectedMode == DropshipAutopilotMode.Cycle;
        _window.RecallOnlyButton.Selected = _selectedMode == DropshipAutopilotMode.RecallOnly;
        _window.LandingZones.Disabled |= _selectedMode == DropshipAutopilotMode.RecallOnly;
        _window.DelayHint.Text = Loc.GetString("rmc-dropship-delay-range",
            ("min", console.MinDelaySeconds), ("max", console.MaxDelaySeconds));
        RefreshActions(console, selected);
    }

    private void SelectDropship(DropshipRemoteControlDropshipEntry selected)
    {
        _selectedComputer = selected.Computer;
        _selectedHangar = selected.RouteHangar;
        _selectedLandingZone = selected.LandingZone;
        _selectedDestination = null;
        _selectedMode = selected.Mode == DropshipAutopilotMode.Disabled ? DropshipAutopilotMode.Cycle : selected.Mode;
        if (_window != null)
            _window.DelayEdit.Text = selected.DelaySeconds.ToString();
    }

    private void SelectMode(DropshipAutopilotMode mode)
    {
        _selectedMode = mode;
        Refresh();
    }

    private void SelectDestination(RMCCrtOptionButton selector, int id, ref NetEntity? selection)
    {
        if (!_options.TryGetValue(selector, out var options) || id < 0 || id >= options.Count)
            return;

        selection = options[id].Id;
        Refresh();
    }

    private NetEntity? FillDestinations(
        RMCCrtOptionButton selector,
        List<DropshipRemoteControlDestinationEntry> destinations,
        NetEntity dropship,
        NetEntity? selection,
        NetEntity? preferred)
    {
        var available = destinations.Where(d => d.AvailableDropships.Contains(dropship)).ToList();
        if (available.All(d => d.Id != selection))
        {
            var preferredEntry = available.FirstOrDefault(d => d.Id == preferred);
            var fallback = available.FirstOrDefault(d => d.Primary && !Occupied(d, dropship));
            if (fallback.Id == default)
                fallback = available.FirstOrDefault(d => !Occupied(d, dropship));
            selection = preferredEntry.Id != default ? preferredEntry.Id : fallback.Id != default ? fallback.Id : null;
        }

        var options = new List<(NetEntity, string)>();
        foreach (var destination in available)
        {
            var label = destination.Name;
            if (destination.Primary)
                label += Loc.GetString("rmc-dropship-remote-ui-primary");
            if (destination.OccupiedBy is { } occupant)
                label += Loc.GetString(occupant == dropship
                    ? "rmc-dropship-remote-ui-current"
                    : "rmc-dropship-remote-ui-occupied");
            options.Add((destination.Id, label));
        }

        FillOptions(selector, options, selection);
        selector.Disabled = options.Count == 0;
        return selection;
    }

    private void FillOptions(
        RMCCrtOptionButton selector,
        List<(NetEntity Id, string Label)> options,
        NetEntity? selection)
    {
        if (!_options.TryGetValue(selector, out var previous) || !previous.SequenceEqual(options))
        {
            selector.Clear();
            for (var i = 0; i < options.Count; i++)
                selector.AddItem(options[i].Label, i);
            _options[selector] = options;
        }

        var index = options.FindIndex(option => option.Id == selection);
        if (index >= 0 && selector.SelectedId != index)
            selector.SelectId(index);
    }

    private void RefreshActions(
        DropshipRemoteControlConsoleComponent console,
        DropshipRemoteControlDropshipEntry selected)
    {
        if (_window == null)
            return;

        var hasDropship = selected.Computer != default;
        var busy = selected.Crashed || selected.InFlight;
        var active = hasDropship && selected.Mode != DropshipAutopilotMode.Disabled;
        var destination = console.Destinations.FirstOrDefault(d => d.Id == _selectedDestination);
        var hangar = console.Hangars.FirstOrDefault(d => d.Id == _selectedHangar);
        var landingZone = console.LandingZones.FirstOrDefault(d => d.Id == _selectedLandingZone);
        _window.ManualLaunchButton.Disabled = !hasDropship || busy || destination.Id == default ||
            Occupied(destination, selected.Dropship) || destination.OccupiedBy == selected.Dropship;
        _window.ApplyButton.Disabled = !hasDropship || selected.Crashed || hangar.Id == default ||
            Occupied(hangar, selected.Dropship) || (_selectedMode == DropshipAutopilotMode.Cycle &&
            (landingZone.Id == default || Occupied(landingZone, selected.Dropship)));
        _window.DisableButton.Disabled = !active;
        _window.LaunchNowButton.Disabled = !active || busy;
        _window.RecallNowButton.Disabled = !active || busy;
        _window.CycleButton.Disabled = !hasDropship;
        _window.RecallOnlyButton.Disabled = !hasDropship;
        _window.DelayEdit.Editable = hasDropship;
    }

    private static bool Occupied(DropshipRemoteControlDestinationEntry destination, NetEntity dropship)
    {
        return destination.OccupiedBy is { } occupant && occupant != dropship;
    }

    private void ConfigureAutopilot()
    {
        if (_window == null || _selectedComputer is not { } computer || _selectedHangar is not { } hangar ||
            !EntMan.TryGetComponent(Owner, out DropshipRemoteControlConsoleComponent? console))
        {
            return;
        }

        var delay = int.TryParse(_window.DelayEdit.Text, out var parsed) ? parsed : console.DefaultDelaySeconds;
        delay = Math.Clamp(delay, console.MinDelaySeconds, console.MaxDelaySeconds);
        _window.DelayEdit.Text = delay.ToString();
        SendPredictedMessage(new DropshipRemoteAutopilotConfigureMsg(
            computer, _selectedMode, hangar, _selectedLandingZone, delay));
    }
}
