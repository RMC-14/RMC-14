using System.Linq;
using Content.Client._RMC14.TacticalMap;
using Content.Client._RMC14.TacticalMap.Controls;
using Content.Client._RMC14.TacticalMap.UI;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Xenonids.Construction.Tunnel;

[UsedImplicitly]
public sealed class SelectDestinationTunnelBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly Color RouteColor = Color.FromHex("#F535AA");

    private readonly TacticalMapSystem _tacticalMap;
    private readonly Dictionary<NetEntity, string> _tunnelNames = new();
    private readonly Dictionary<NetEntity, Vector2i> _tunnelIndices = new();
    private readonly List<TacticalMapBlipView> _filtered = new();

    private SelectDestinationTunnelWindow? _window;
    private TacticalMapViewBinder? _binder;
    private NetEntity? _currentTunnel;
    private NetEntity? _selectedTunnel;
    private bool _onlyTunnels = true;

    public SelectDestinationTunnelBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _tacticalMap = EntMan.System<TacticalMapSystem>();
    }

    protected override void Open()
    {
        base.Open();

        if (_window is { IsOpen: true })
            return;

        if (_player.LocalEntity is not { } viewer)
        {
            Close();
            return;
        }

        _window = this.CreateWindow<SelectDestinationTunnelWindow>();
        var view = _window.View;
        view.Configure(false, true, _tacticalMap.LineLimit, _tacticalMap.LabelMaxLength);

        _binder = new TacticalMapViewBinder(view, viewer, null) { BlipFilter = FilterBlips };
        view.SetTheme(TacticalMapTheme.Xeno, Loc.GetString("xeno-ui-select-destination-tunnel-title"));
        view.BlipActivated += OnBlipActivated;

        var panel = _window.Panel;
        panel.TunnelSelected += tunnel => Select(tunnel, true);
        panel.TravelPressed += Travel;
        panel.OnlyTunnelsToggled += () =>
        {
            _onlyTunnels = !_onlyTunnels;
            UpdatePanel();
            _binder?.InvalidateBlips();
        };

        UpdatePanel();
        _binder.Refresh(true);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not SelectDestinationTunnelInterfaceState tunnelState)
            return;

        _tunnelNames.Clear();
        _currentTunnel = null;
        foreach (var (name, tunnel) in tunnelState.HiveTunnels)
        {
            _tunnelNames[tunnel] = name;
            if (EntMan.GetEntity(tunnel) == Owner)
                _currentTunnel = tunnel;
        }

        if (_selectedTunnel is { } selected && (selected == _currentTunnel || !_tunnelNames.ContainsKey(selected)))
            _selectedTunnel = null;

        _window?.Panel.SetTunnels(tunnelState.HiveTunnels
            .Where(pair => pair.Value != _currentTunnel)
            .OrderBy(pair => pair.Key.ToLowerInvariant())
            .Select(pair => (pair.Key, pair.Value)));

        UpdatePanel();
        _binder?.InvalidateBlips();
    }

    private void FilterBlips(List<TacticalMapBlipView> blips)
    {
        _filtered.Clear();
        _tunnelIndices.Clear();
        foreach (var view in blips)
        {
            if (_tunnelNames.TryGetValue(view.Entity, out var name))
            {
                _tunnelIndices.TryAdd(view.Entity, view.Blip.Indices);
                _filtered.Add(view with { Blip = view.Blip with { Name = name } });
                continue;
            }

            if (!_onlyTunnels)
                _filtered.Add(view);
        }

        blips.Clear();
        blips.AddRange(_filtered);
        UpdateRoute();
    }

    private void UpdateRoute()
    {
        if (_window is not { } window)
            return;

        Vector2i? from = null;
        if (_currentTunnel is { } current && _tunnelIndices.TryGetValue(current, out var currentIndices))
            from = currentIndices;
        else if (window.View.SelectedMap is { } map && _tacticalMap.TryGetTileIndices(Owner, map, out var ownerIndices))
            from = ownerIndices;

        Vector2i? to = null;
        if (_selectedTunnel is { } selected && _tunnelIndices.TryGetValue(selected, out var selectedIndices))
            to = selectedIndices;

        window.View.Map.SetRoute(from, to, RouteColor);
    }

    private void OnBlipActivated(NetEntity entity)
    {
        if (entity != _currentTunnel && _tunnelNames.ContainsKey(entity))
            Select(entity, false);
    }

    private void Select(NetEntity tunnel, bool center)
    {
        _selectedTunnel = tunnel;
        UpdatePanel();
        _binder?.InvalidateBlips();

        if (center && _window != null && _tunnelIndices.TryGetValue(tunnel, out var indices))
            _window.View.Map.CenterOn(indices);
    }

    private void UpdatePanel()
    {
        if (_window == null)
            return;

        string? current = _currentTunnel is { } currentTunnel ? _tunnelNames.GetValueOrDefault(currentTunnel) : null;
        string? selected = _selectedTunnel is { } selectedTunnel ? _tunnelNames.GetValueOrDefault(selectedTunnel) : null;
        _window.Panel.SetState(current, selected, _selectedTunnel, _onlyTunnels);
    }

    private void Travel()
    {
        if (_selectedTunnel is not { } destination)
            return;

        SendMessage(new TraverseXenoTunnelMessage(destination));
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _binder?.Dispose();
        _binder = null;
    }
}
