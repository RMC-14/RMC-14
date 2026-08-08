using Content.Client.Stylesheets;
using Content.Shared._RMC14.Elevators;
using Content.Shared.Shuttles.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Elevators;

[UsedImplicitly]
public sealed class ElevatorPanelBui: BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [ViewVariables]
    private ElevatorPanelWindow? _window;

    private ElevatorTravellingMsg? _travelling;

    private readonly Dictionary<ElevatorButton, string> _destinations = new();

    public ElevatorPanelBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        OpenWindow();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is ElevatorTravellingMsg { } travel)
        {
            SetStatus(travel);
            return;
        }

        if (message is ElevatorDestinationsMsg { } dest)
        {
            SetStatus(dest);
            return;
        }

    }

    private void OpenWindow()
    {
        if (_window != null)
            return;

        _window ??= this.CreateWindow<ElevatorPanelWindow>();
        _window.OnClose += OnClose;

        if (!_entities.TryGetComponent<RMCElevatorPanelComponent>(Owner, out var panel))
            return;

        _window.Title = panel.CallOnly ? Loc.GetString("rmc-elevator-call-name") : Loc.GetString("rmc-elevator-panel-name");

        _destinations.Clear();
        _entities.System<RMCElevatorSystem>().Uis.Add(this);
    }

    private void OnClose()
    {
        _entities.System<RMCElevatorSystem>().Uis.Remove(this);
        Close();
    }

    public void SetStatus(ElevatorDestinationsMsg destinations)
    {
        if (_window == null)
            return;

        _window.ButtonGrid.DisposeAllChildren();
        _destinations.Clear();

        ElevatorButton EleButton(string name, bool disabled)
        {
            var button = new ElevatorButton();

            button.DestinationLabel.Text = $"[font size=14][bold]{name}[/bold][/font]";
            button.DestinationButton.Disabled = disabled;
            button.DestinationButton.StyleClasses.Add(StyleBase.ButtonSquare);

            return button;
        }

        if (!_entities.TryGetComponent<RMCElevatorPanelComponent>(Owner, out var panel))
            return;

        if (panel.CallOnly)
        {
            if (panel.LinkedDestination == null)
                return;

            var disable = destinations.CurrDestination == null ? false : (destinations.CurrDestination == _entities.GetNetEntity(panel.LinkedDestination));
            var button = EleButton(Loc.GetString("rmc-elevator-call-button-text"), disable);
            button.DestinationButton.OnPressed += _ => { SendPredictedMessage(new ElevatorSendMsg(_entities.GetNetEntity(panel.LinkedDestination.Value))); };
            _destinations[button] = Loc.GetString("rmc-elevator-call-button-text");
            _window.ButtonGrid.AddChild(button);
            return;
        }

        foreach (var destination in destinations.Destinations)
        {
            var name = destination.Name;

            var button = EleButton(name, destinations.CurrDestination == destination.Id);
            button.DestinationButton.OnPressed += _ => { SendPredictedMessage(new ElevatorSendMsg(destination.Id)); };
            _destinations[button] = name;
            _window.ButtonGrid.AddChild(button);
        }
    }

    public void SetStatus(ElevatorTravellingMsg travelling)
    {
        if (_window == null)
            return;

        _travelling = travelling;

        var time = Math.Ceiling((travelling.Time.End - _timing.CurTime).TotalSeconds);
        if (time < 0.01)
            time = 0;

        _window.StatusLabel.Text = travelling.State switch
        {
            FTLState.Available => Loc.GetString("rmc-elevator-status-available"),
            FTLState.Starting => Loc.GetString("rmc-elevator-status-starting"),
            FTLState.Travelling => Loc.GetString("rmc-elevator-status-travelling"),
            FTLState.Arriving => Loc.GetString("rmc-elevator-status-arriving"),
            FTLState.Cooldown => Loc.GetString("rmc-elevator-status-cooldown"),
            _ => Loc.GetString("rmc-elevator-status-unknown")
        };

        if (travelling.State != FTLState.Available)
            _window.StatusProgress.Text = $"[font size=14][bold]{time}[/bold][/font]";
        else
            _window.StatusProgress.Text = $"[font size=14][bold]{Loc.GetString("rmc-elevator-status-progress-availible")}[/bold][/font]";

        if (travelling.State == FTLState.Cooldown || travelling.State == FTLState.Available)
            _window.LocationStatus.Text = Loc.GetString("rmc-elevator-location");
        else
            _window.LocationStatus.Text = Loc.GetString("rmc-elevator-travelling");

        _window.LocationLabel.Text = $"[font size=14][bold]{travelling.Destination}[/bold][/font]";
    }

    public override void Update()
    {
        if (_window == null || _window.Disposed)
            return;

        if (_travelling != null)
            SetStatus(_travelling);
    }
}


