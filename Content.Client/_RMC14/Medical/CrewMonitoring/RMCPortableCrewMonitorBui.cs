using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Medical.CrewMonitoring;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Medical.CrewMonitoring;

public sealed class RMCPortableCrewMonitorBui : RMCPopOutBui<RMCPortableCrewMonitorWindow>
{
    private readonly IPrototypeManager _prototypes;
    private readonly SpriteSystem _sprite;

    protected override RMCPortableCrewMonitorWindow? Window { get; set; }

    public RMCPortableCrewMonitorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _prototypes = IoCManager.Resolve<IPrototypeManager>();
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        Window = this.CreatePopOutableWindow<RMCPortableCrewMonitorWindow>();
        Window.Search.OnTextChanged += _ => RefreshSignals();
        Window.ScanButton.OnPressed += _ => SendMessage(new RMCPortableCrewMonitorScanBuiMsg());
        Refresh();
    }

    public void Refresh()
    {
        RefreshSignals();
        RefreshTracking();
    }

    public void RefreshSignals()
    {
        if (Window == null || !EntMan.TryGetComponent(Owner, out RMCPortableCrewMonitorComponent? monitor))
            return;

        Window.ScanButton.Disabled = monitor.Scanning;
        Window.ScanButton.Text = Loc.GetString(monitor.Scanning
            ? "rmc-portable-crew-monitor-scanning"
            : "rmc-portable-crew-monitor-scan");
        Window.Signals.RemoveAllChildren();

        if (monitor.Scanning)
        {
            SetState("rmc-portable-crew-monitor-scanning");
            return;
        }

        if (!monitor.HasScanned)
        {
            SetState("rmc-portable-crew-monitor-scan-prompt");
            return;
        }

        var search = Window.Search.Text.Trim();
        var signals = monitor.Signals.Where(entry =>
            search.Length == 0 ||
            entry.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
            entry.JobTitle.Contains(search, StringComparison.CurrentCultureIgnoreCase));

        var count = 0;
        foreach (var entry in signals)
        {
            count++;
            var row = new RMCPortableCrewMonitorRow
            {
                Pressed = monitor.Selected == entry.Id,
            };
            row.NameLabel.Text = entry.Name;
            row.JobLabel.Text = entry.JobTitle;
            row.StatusLabel.Text = RMCCrewMonitorUIHelpers.GetStatusName(entry.State);
            row.StatusLabel.FontColorOverride = RMCCrewMonitorUIHelpers.GetStatusColor(entry.State);
            if (_prototypes.TryIndex(entry.JobIcon, out JobIconPrototype? jobIcon))
                row.JobIcon.Texture = _sprite.Frame0(jobIcon.Icon);

            var target = entry.Id;
            row.OnPressed += _ => SendMessage(new RMCPortableCrewMonitorSelectBuiMsg(target));
            Window.Signals.AddChild(row);
        }

        if (count == 0)
        {
            SetState("rmc-portable-crew-monitor-empty");
            return;
        }

        Window.GrowToFitSignals();
        Window.StateLabel.Visible = false;
        Window.SignalsScroll.Visible = true;
    }

    public void RefreshTracking()
    {
        if (Window == null ||
            !EntMan.TryGetComponent(Owner, out RMCPortableCrewMonitorComponent? monitor) ||
            !EntMan.TryGetComponent(Owner, out RMCPortableCrewMonitorTrackingComponent? tracking))
        {
            return;
        }

        var selectedName = Loc.GetString("rmc-portable-crew-monitor-unknown-target");
        if (monitor.Selected is { } selected)
        {
            foreach (var signal in monitor.Signals)
            {
                if (signal.Id != selected)
                    continue;

                selectedName = signal.Name;
                break;
            }
        }

        Window.Radar.SetTarget(
            monitor.Selected != null,
            selectedName,
            tracking.Offset,
            monitor.RadarRange,
            tracking.DirectionOnly);
    }

    private void SetState(string locId)
    {
        if (Window == null)
            return;

        Window.SignalsScroll.Visible = false;
        Window.StateLabel.Text = Loc.GetString(locId);
        Window.StateLabel.Visible = true;
    }
}
