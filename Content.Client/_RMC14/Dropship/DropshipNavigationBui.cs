using Content.Client.Message;
using Content.Shared._RMC14.Dropship;
using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Dropship;

[UsedImplicitly]
public sealed class DropshipNavigationBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [ViewVariables]
    private DropshipNavigationWindow? _window;

    private readonly Dictionary<DropshipButton, string> _destinations = new();
    private NetEntity? _selected;

    public DropshipNavigationBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        OpenWindow();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        OpenWindow();

        switch (state)
        {
            case DropshipNavigationDestinationsBuiState s:
                Set(s);
                return;
            case DropshipNavigationTravellingBuiState s:
                Set(s);
                return;
        }
    }

    private void OpenWindow()
    {
        if (_window != null)
            return;

        _window = this.CreateWindow<DropshipNavigationWindow>();
        _window.OnClose += OnClose;
        SetFlightHeader(Loc.GetString("rmc-dropship-nav-flight-controls"));
        SetDoorHeader(Loc.GetString("rmc-dropship-nav-door-controls"));
        SetRemoteControlHeader(Loc.GetString("rmc-dropship-nav-remote-control"));
        SetLaunchAlarmHeader(Loc.GetString("rmc-dropship-nav-launch-alarm"));

        if (_entities.TryGetComponent(Owner, out TransformComponent? transform) &&
            _entities.TryGetComponent(transform.ParentUid, out MetaDataComponent? metaData))
        {
            _window.Title = $"{metaData.EntityName} {_window.Title}";
        }

        _window.CancelButton.Button.OnPressed += _ =>
        {
            SetLaunchDisabled(true);
            SetCancelDisabled(true);
            _selected = null;
            ResetDestinationButtons();
            CancelFlyby();
        };

        _window.LaunchButton.Button.OnPressed += _ =>
        {
            if (_selected != null)
                SendPredictedMessage(new DropshipNavigationLaunchMsg(_selected.Value));

            SetLaunchDisabled(true);
            _selected = null;
            ResetDestinationButtons();
        };

        _window.LockdownButton.Button.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.None));
        _window.LockdownButtonAft.Button.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.Aft));
        _window.LockdownButtonPort.Button.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.Port));
        _window.LockdownButtonStarboard.Button.OnPressed += _ => SendPredictedMessage(new DropshipLockdownMsg(DoorLocation.Starboard));
        _window.RemoteControlButton.Button.OnPressed += _ => SendPredictedMessage(new DropshipRemoteControlToggleMsg());
        _window.LaunchAlarmButton.Button.OnPressed += _ => SendPredictedMessage(new DropshipLaunchAlarmToggleMsg());
        _entities.System<DropshipSystem>().Uis.Add(this);
    }

    private void OnClose()
    {
        _entities.System<DropshipSystem>().Uis.Remove(this);
        Close();
    }

    private void Set(DropshipNavigationDestinationsBuiState destinations)
    {
        if (_window == null)
            return;

        SetFlightHeader(Loc.GetString("rmc-dropship-nav-flight-controls"));

        _window.DestinationsContainer.Visible = true;
        _window.ProgressBarContainer.Visible = false;
        _window.CancelButton.Visible = true;
        _window.LaunchButton.Visible = true;

        _window.DestinationsContainer.DisposeAllChildren();

        DropshipButton DestinationButton(string name, bool disabled, Action onPressed)
        {
            var button = new DropshipButton();

            button.Text = name;
            button.Disabled = disabled;
            button.BorderColor = Color.Transparent;
            button.BorderThickness = new Thickness(0);
            button.Button.ToggleMode = false;
            button.Button.OnPressed += _ =>
            {
                ResetDestinationButtons();
                button.Text = $"> {name}";
                SetLaunchDisabled(false);
                SetCancelDisabled(false);
                onPressed();
            };

            return button;
        }

        _destinations.Clear();
        if (destinations.FlyBy is { } flyBy)
        {
            var flyByName = Loc.GetString("rmc-dropship-nav-flyby");
            var flyByButton = DestinationButton(flyByName, false, () => _selected = flyBy);
            _destinations[flyByButton] = flyByName;
            _window.DestinationsContainer.AddChild(flyByButton);
        }

        foreach (var destination in destinations.Destinations)
        {
            var name = destination.Name;
            if (destination.Primary)
                name = Loc.GetString("rmc-dropship-nav-destination-primary", ("name", name));

            var button = DestinationButton(name, destination.Occupied, () => _selected = destination.Id);

            _destinations[button] = name;
            _window.DestinationsContainer.AddChild(button);
        }

        RefreshDoorLockStatus(destinations.DoorLockStatus);
        SetRemoteControl(destinations.RemoteControlStatus);
        RefreshLaunchAlarmStatus(destinations.LaunchAlarmStatus);
    }

    private void Set(DropshipNavigationTravellingBuiState travelling)
    {
        if (_window == null)
            return;

        _window.DestinationsContainer.Visible = false;
        _window.ProgressBarContainer.Visible = true;
        _window.LaunchButton.Visible = false;
        _window.ProgressBar.Margin = new Thickness(0, 5, 0, 0);

        if (travelling.Destination == travelling.DepartureLocation)
            _window.CancelButton.Visible = true;
        else
            _window.CancelButton.Visible = false;

        var time = Math.Ceiling((travelling.Time.End - _timing.CurTime).TotalSeconds);
        if (time < 0.01)
            time = 0;

        var destination = travelling.Destination;
        string Msg(string msg) => $"[color=#02E74E][bold]{msg}[/bold][/color]";

        switch (travelling.State)
        {
            case FTLState.Starting:
                SetFlightHeader(Loc.GetString("rmc-dropship-nav-launch-in-progress"));
                _window.ProgressBarHeader.SetMarkup(Msg(Loc.GetString("rmc-dropship-nav-launching-in", ("time", time), ("destination", destination))));
                SetLockDownDisabled(false);
                break;
            case FTLState.Travelling:
                SetFlightHeader(Loc.GetString("rmc-dropship-nav-in-flight", ("destination", destination)));
                _window.ProgressBarHeader.SetMarkup(Msg(Loc.GetString("rmc-dropship-nav-time-until-destination", ("time", time))));
                SetLockDownDisabled(true);
                SetCancelDisabled(false);
                break;
            case FTLState.Arriving:
                SetFlightHeader(Loc.GetString("rmc-dropship-nav-final-approach", ("destination", destination)));
                _window.ProgressBarHeader.SetMarkup(Msg(Loc.GetString("rmc-dropship-nav-time-until-landing", ("time", time))));
                SetLockDownDisabled(true);
                SetCancelDisabled(true);
                break;
            case FTLState.Cooldown:
                SetFlightHeader(Loc.GetString("rmc-dropship-nav-refueling"));
                _window.ProgressBarHeader.SetMarkup(Msg(Loc.GetString("rmc-dropship-nav-ready-in", ("time", time))));
                SetLockDownDisabled(false);
                SetCancelDisabled(true);
                break;
            default:
                return;
        }

        RefreshDoorLockStatus(travelling.DoorLockStatus);
        SetRemoteControl(travelling.RemoteControlStatus);
        RefreshLaunchAlarmStatus(travelling.LaunchAlarmStatus);

        var startEndTime = travelling.Time;
        _window.ProgressBar.MinValue = 0;
        _window.ProgressBar.MaxValue = (float) startEndTime.Length.TotalSeconds;
        _window.ProgressBar.SetAsRatio(1 - startEndTime.ProgressAt(_timing.CurTime));
    }

    private void SetFlightHeader(string label)
    {
        _window?.Header.SetMarkup($"[color=#0BDC49][font size=16][bold]{label}[/bold][/font][/color]");
    }

    private void SetDoorHeader(string label)
    {
        _window?.DoorHeader.SetMarkup($"[color=#0BDC49][font size=16][bold]{label}[/bold][/font][/color]");
    }

    private void SetRemoteControlHeader(string label)
    {
        _window?.RemoteControlHeader.SetMarkup($"[color=#0BDC49][font size=16][bold]{label}[/bold][/font][/color]");
    }

    private void SetLaunchAlarmHeader(string label)
    {
        _window?.LaunchAlarmHeader.SetMarkup($"[color=#0BDC49][font size=16][bold]{label}[/bold][/font][/color]");
    }

    private void SetLaunchDisabled(bool disabled)
    {
        if (_window == null)
            return;

        _window.LaunchButton.Button.Disabled = disabled;
    }

    private void SetCancelDisabled(bool disabled)
    {
        if (_window == null)
            return;

        _window.CancelButton.Button.Disabled = disabled;
    }

    private void SetLockDownDisabled(bool disabled)
    {
        if (_window == null)
            return;

        _window.LockdownButton.Button.Disabled = disabled;
        _window.LockdownButtonAft.Button.Disabled = disabled;
        _window.LockdownButtonPort.Button.Disabled = disabled;
        _window.LockdownButtonStarboard.Button.Disabled = disabled;
    }

    private void SetRemoteControl(bool status)
    {
        if (_window == null)
            return;

        _window.RemoteControlButton.Text = Loc.GetString(status ? "rmc-dropship-nav-enabled" : "rmc-dropship-nav-disabled");
    }

    private void ResetDestinationButtons()
    {
        if (_window == null)
            return;

        foreach (var destination in _window.DestinationsContainer.Children)
        {
            if (destination is not DropshipButton button ||
                !_destinations.TryGetValue(button, out var name))
            {
                continue;
            }

            button.Text = name;
        }
    }

    private void CancelFlyby()
    {
        if (_window == null)
            return;

        SendPredictedMessage(new DropshipNavigationCancelMsg());
    }

    private void RefreshDoorLockStatus(Dictionary<DoorLocation, bool> dooorLockStatus)
    {
        if (_window == null)
            return;

        dooorLockStatus.TryGetValue(DoorLocation.Aft, out var aftStatus);
        dooorLockStatus.TryGetValue(DoorLocation.Port, out var portStatus);
        dooorLockStatus.TryGetValue(DoorLocation.Starboard, out var starboardStatus);
        var lockdownStatus = aftStatus && portStatus && starboardStatus;

        _window.LockdownButton.Text = Loc.GetString(lockdownStatus ? "rmc-dropship-nav-lift-lockdown" : "rmc-dropship-nav-lockdown");
        _window.LockdownButtonAft.Text = Loc.GetString(aftStatus ? "rmc-dropship-nav-unlock-aft" : "rmc-dropship-nav-lock-aft");
        _window.LockdownButtonPort.Text = Loc.GetString(portStatus ? "rmc-dropship-nav-unlock-port" : "rmc-dropship-nav-lock-port");
        _window.LockdownButtonStarboard.Text = Loc.GetString(starboardStatus ? "rmc-dropship-nav-unlock-starboard" : "rmc-dropship-nav-lock-starboard");
    }

    private void RefreshLaunchAlarmStatus(bool launchAlarmStatus)
    {
        if (_window == null)
            return;

        _window.LaunchAlarmButton.Text = Loc.GetString(launchAlarmStatus ? "rmc-dropship-nav-stop-alarm" : "rmc-dropship-nav-start-alarm");
    }

    public void Update()
    {
        if (_window == null || _window.Disposed)
            return;

        if (State is DropshipNavigationTravellingBuiState s)
            Set(s);
    }
}
