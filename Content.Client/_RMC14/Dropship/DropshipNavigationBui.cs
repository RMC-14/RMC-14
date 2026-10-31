using System.Linq;
using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared._RMC14.Dropship;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Dropship;

[UsedImplicitly]
public sealed class DropshipNavigationBui : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private DropshipNavigationWindow? _window;
    private readonly Dictionary<NetEntity, DropshipDestinationButton> _destinations = new();
    private List<Destination> _lastDestinations = new();
    private NetEntity? _lastFlyBy;
    private NetEntity? _selected;
    private bool _showPrimary;

    public DropshipNavigationBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _destinations.Clear();
        _lastDestinations.Clear();
        _lastFlyBy = null;
        _selected = null;
        _window = this.CreateWindow<DropshipNavigationWindow>();
        _window.CancelButton.OnPressed += _ =>
        {
            _selected = null;
            SendPredictedMessage(new DropshipNavigationCancelMsg());
            Refresh();
        };
        _window.LaunchButton.OnPressed += _ =>
        {
            if (_selected is { } selected)
                SendPredictedMessage(new DropshipNavigationLaunchMsg(selected));
            _selected = null;
            Refresh();
        };
        _window.LockdownButton.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.None));
        _window.LockdownButtonAft.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.Aft));
        _window.LockdownButtonPort.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.Port));
        _window.LockdownButtonStarboard.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.Starboard));
        _window.LaunchAlarmButton.OnPressed += _ => SendPredictedMessage(new DropshipLaunchAlarmToggleMsg());
        _window.AutopilotDisableButton.OnPressed += _ => SendPredictedMessage(new DropshipNavigationAutopilotDisableMsg());
        EntMan.System<DropshipSystem>().Uis.Add(this);
        _window.OnClose += () => EntMan.System<DropshipSystem>().Uis.Remove(this);
        Refresh();
    }

    public void Refresh()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out DropshipNavigationComputerComponent? computer))
            return;

        var title = computer.NavigationWindowTitle == "Flight Computer"
            ? Loc.GetString("rmc-dropship-navigation-title")
            : computer.NavigationWindowTitle;
        if (EntMan.TryGetComponent(Owner, out TransformComponent? xform) &&
            EntMan.TryGetComponent(xform.GridUid, out MetaDataComponent? metadata))
        {
            title = $"{metadata.EntityName} — {title}";
        }
        _window.Title = title;
        _window.IndividualDoorControlsContainer.Visible = computer.ShowIndividualDoorControls;
        _window.LaunchAlarmContainer.Visible = computer.ShowLaunchAlarmControls;
        _window.DestinationsContainer.Visible = computer.TravelState == null;
        _window.ProgressBarContainer.Visible = computer.TravelState != null;
        _window.LaunchButton.Visible = computer.TravelState == null;

        if (computer.TravelState == null)
            RefreshDestinations(computer);
        RefreshDoors(computer);
        RefreshAutopilot(computer);
        Update();
    }

    private void RefreshDestinations(DropshipNavigationComputerComponent computer)
    {
        if (_window == null)
            return;

        var flyBy = computer.ShowFlyByDestination ? computer.FlyBy : null;
        if (_lastFlyBy != flyBy || _showPrimary != computer.ShowPrimaryDestinationLabel ||
            !_lastDestinations.SequenceEqual(computer.Destinations))
        {
            _window.DestinationsContainer.DisposeAllChildren();
            _destinations.Clear();
            _lastDestinations = new List<Destination>(computer.Destinations);
            _lastFlyBy = flyBy;
            _showPrimary = computer.ShowPrimaryDestinationLabel;
            if (flyBy is { } id)
                AddDestination(id, Loc.GetString("rmc-dropship-flyby"), false);
            foreach (var destination in computer.Destinations)
            {
                var name = destination.Name;
                if (destination.Primary && computer.ShowPrimaryDestinationLabel)
                    name += Loc.GetString("rmc-dropship-remote-ui-primary");
                AddDestination(destination.Id, name, destination.Occupied);
            }
        }

        if (_selected is { } selected &&
            (!_destinations.TryGetValue(selected, out var selectedRow) || selectedRow.Button.Disabled))
            _selected = null;
        foreach (var (id, row) in _destinations)
            row.Button.Selected = id == _selected;
        _window.LaunchButton.Disabled = _selected == null;
        _window.CancelButton.Visible = true;
        _window.CancelButton.Disabled = _selected == null;
    }

    private void AddDestination(NetEntity id, string name, bool disabled)
    {
        var row = new DropshipDestinationButton();
        row.Button.Text = name;
        row.Button.Disabled = disabled;
        row.Button.OnPressed += _ =>
        {
            _selected = id;
            Refresh();
        };
        _destinations[id] = row;
        _window?.DestinationsContainer.AddChild(row);
    }

    private void RefreshDoors(DropshipNavigationComputerComponent computer)
    {
        if (_window == null)
            return;

        var locks = computer.DoorLockStatus;
        var aft = locks.GetValueOrDefault(DoorLocation.Aft);
        var port = locks.GetValueOrDefault(DoorLocation.Port);
        var starboard = locks.GetValueOrDefault(DoorLocation.Starboard);
        _window.LockdownButton.Text = Loc.GetString(aft && port && starboard
            ? "rmc-dropship-unlock-all" : "rmc-dropship-lock-all");
        _window.LockdownButtonAft.Text = Loc.GetString(aft ? "rmc-dropship-unlock-aft" : "rmc-dropship-lock-aft");
        _window.LockdownButtonPort.Text = Loc.GetString(port ? "rmc-dropship-unlock-port" : "rmc-dropship-lock-port");
        _window.LockdownButtonStarboard.Text = Loc.GetString(starboard ? "rmc-dropship-unlock-starboard" : "rmc-dropship-lock-starboard");
        _window.LaunchAlarmButton.Text = Loc.GetString(computer.LaunchAlarmStatus
            ? "rmc-dropship-alarm-stop" : "rmc-dropship-alarm-start");
        var disabled = computer.TravelState is FTLState.Travelling or FTLState.Arriving;
        _window.LockdownButton.Disabled = disabled;
        _window.LockdownButtonAft.Disabled = disabled;
        _window.LockdownButtonPort.Disabled = disabled;
        _window.LockdownButtonStarboard.Disabled = disabled;
    }

    private void RefreshAutopilot(DropshipNavigationComputerComponent computer)
    {
        if (_window == null)
            return;

        _window.AutopilotContainer.Visible = computer.ShowAutopilotControls && computer.Autopilot != null;
        if (computer.Autopilot is not { } status)
            return;
        _window.AutopilotHeader.Text = Loc.GetString("rmc-dropship-autopilot-mode",
            ("mode", DropshipAutopilotText.Mode(status.Mode)));
        _window.AutopilotStatus.Text = DropshipAutopilotText.Status(status.Status);
        _window.AutopilotStatus.Tone = DropshipAutopilotText.Tone(status.Status);
        _window.AutopilotDetails.Text = DropshipAutopilotText.Details(status.StatusDetails, status.DepartInSeconds);
    }

    public new void Update()
    {
        if (_window == null || !EntMan.TryGetComponent(Owner, out DropshipNavigationComputerComponent? computer) ||
            computer.TravelState is not { } state)
        {
            return;
        }

        var seconds = Math.Max(0, (int)Math.Ceiling((computer.TravelTime.End - _timing.CurTime).TotalSeconds));
        var key = state switch
        {
            FTLState.Starting => "rmc-dropship-flight-starting",
            FTLState.Travelling => "rmc-dropship-flight-travelling",
            FTLState.Arriving => "rmc-dropship-flight-arriving",
            FTLState.Cooldown => "rmc-dropship-flight-refueling",
            _ => "rmc-dropship-flight-controls",
        };
        _window.ProgressBarHeader.Text = Loc.GetString(key,
            ("destination", computer.TravelDestination), ("seconds", seconds));
        _window.CancelButton.Visible = computer.TravelDestination == computer.TravelDeparture;
        _window.CancelButton.Disabled = state != FTLState.Travelling;
        var duration = computer.TravelTime.Length.TotalSeconds;
        _window.ProgressBar.MinValue = 0;
        _window.ProgressBar.MaxValue = (float)Math.Max(1, duration);
        _window.ProgressBar.Value = (float)Math.Clamp(duration - seconds, 0, Math.Max(1, duration));
    }
}
