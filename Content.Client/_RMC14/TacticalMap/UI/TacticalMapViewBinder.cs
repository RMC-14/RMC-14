using System.Linq;
using System.Numerics;
using Content.Client._RMC14.TacticalMap.Controls;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared.Ghost;
using Robust.Client.Player;

namespace Content.Client._RMC14.TacticalMap.UI;

public sealed class TacticalMapViewBinder : IDisposable
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly ISawmill _sawmill;
    private readonly TacticalMapSystem _tacticalMap;
    private readonly TacticalMapView _view;
    private readonly Action<BoundUserInterfaceMessage>? _send;
    private readonly List<EntityUid> _channels = new();
    private readonly List<TacticalMapMapOption> _maps = new();
    private readonly List<TacticalMapBlipView> _blips = new();
    private readonly List<TacticalMapCanvasLayer> _layers = new();
    private readonly List<TacticalMapCanvasLayer> _visibleLayers = new();
    private readonly HashSet<EntityUid> _hiddenLayers = new();
    private readonly Dictionary<EntityUid, TacticalMapCanvasDraft> _drafts = new();
    private EntityUid? _drawTarget;
    private int _revision = -1;
    private int _blipRevision = -1;
    private string? _lastError;

    public EntityUid Viewer { get; }

    private bool CanEdit => _send != null;

    public TacticalMapViewBinder(TacticalMapView view, EntityUid viewer, Action<BoundUserInterfaceMessage>? send)
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("rmc.tactical_map");
        _tacticalMap = _entMan.System<TacticalMapSystem>();
        _view = view;
        _send = send;
        Viewer = viewer;

        var xeno = _entMan.HasComponent<XenoComponent>(viewer);
        _view.SetTheme(
            xeno ? TacticalMapTheme.Xeno : TacticalMapTheme.Marine,
            Loc.GetString(xeno ? "ui-tactical-map-title-hive" : "ui-tactical-map-title"));

        _view.Frame += OnFrame;
        _view.MapSelected += OnMapSelected;
        _view.SubmitPressed += OnSubmitPressed;
        _view.ClearPressed += OnClearPressed;
        _view.UndoPressed += OnUndoPressed;
        _view.DrawTargetSelected += OnDrawTargetSelected;
        _view.LayerVisibilityToggled += OnLayerVisibilityToggled;
        _view.BlipActivated += OnBlipActivated;
        _view.PointActivated += OnPointActivated;
    }

    public void Refresh(bool force = false)
    {
        try
        {
            RefreshView(force);
            _lastError = null;
        }
        catch (Exception e)
        {
            var error = e.ToString();
            if (error == _lastError)
                return;

            _lastError = error;
            _sawmill.Error($"Error refreshing the tactical map for {_entMan.ToPrettyString(Viewer)}:\n{e}");
        }
    }

    private void RefreshView(bool force)
    {
        _view.SetPointMode(CanEdit && IsPointMode());

        var structural = force || _revision != _tacticalMap.Revision;
        if (!structural && _blipRevision == _tacticalMap.BlipRevision)
            return;

        _revision = _tacticalMap.Revision;
        _blipRevision = _tacticalMap.BlipRevision;
        _tacticalMap.TryGetViewerChannels(Viewer, _channels, out var live);

        if (structural)
        {
            _tacticalMap.CollectMaps(_channels, _maps);
            _view.SetMaps(_maps, GetPreferredMap());

            if (_view.SelectedMap is { } selected)
                _tacticalMap.CollectCanvasLayers(CanEdit ? _player.LocalEntity : null, Viewer, _channels, selected, _layers);
            else
                _layers.Clear();

            UpdateCanvas();
        }

        TimeSpan? snapshotTime = null;
        if (_view.SelectedMap is { } map)
            snapshotTime = _tacticalMap.CollectBlips(_channels, live, map, _blips);
        else
            _blips.Clear();

        NetEntity? self = null;
        Vector2i? selfIndices = null;
        if (_view.SelectedMap is { } current &&
            Viewer == _player.LocalEntity &&
            _entMan.TryGetComponent(Viewer, out TacticalMapUserComponent? user) &&
            user.SelfMap == current)
        {
            self = _entMan.GetNetEntity(Viewer);
            selfIndices = user.SelfIndices;
        }

        _view.SetBlips(_blips, self, selfIndices, live, snapshotTime);
    }

    private void UpdateCanvas()
    {
        foreach (var channel in _drafts.Keys.ToList())
        {
            if (!_layers.Any(l => l.Channel == channel))
                _drafts.Remove(channel);
        }

        _hiddenLayers.RemoveWhere(channel => !_layers.Any(l => l.Channel == channel));

        if (_drawTarget is { } target && !_layers.Any(l => l.Channel == target && l.Drawable))
            _drawTarget = null;

        var drawing = CanEdit && _layers.Any(l => l.Drawable);
        if (drawing && _drawTarget == null)
            _drawTarget = _layers.First(l => l.Drawable).Channel;

        foreach (var layer in _layers)
        {
            if (layer.Drawable && _drafts.TryGetValue(layer.Channel, out var draft) && draft.ShouldResetTo(layer))
                draft.ResetFrom(layer);
        }

        PushCanvas();
    }

    private void PushCanvas()
    {
        _visibleLayers.Clear();
        foreach (var layer in _layers)
        {
            if (!_hiddenLayers.Contains(layer.Channel))
                _visibleLayers.Add(layer);
        }

        var drawing = CanEdit && _drawTarget != null;
        var draft = drawing && _drawTarget is { } target ? GetDraft(target) : null;
        _view.SetLayers(_layers, _visibleLayers, _hiddenLayers);
        _view.SetDrawing(drawing, _drawTarget, draft, CurrentLayer);
        _view.SetObjectives(FindObjectives());
    }

    private TacticalMapCanvasLayer? CurrentLayer =>
        _drawTarget is { } target ? _layers.FirstOrDefault(l => l.Channel == target) : null;

    private TacticalMapCanvasDraft? CurrentDraft =>
        _drawTarget is { } target && _drafts.TryGetValue(target, out var draft) ? draft : null;

    private TacticalMapCanvasDraft GetDraft(EntityUid channel)
    {
        if (_drafts.TryGetValue(channel, out var draft))
            return draft;

        draft = new TacticalMapCanvasDraft(channel);
        if (_layers.FirstOrDefault(l => l.Channel == channel) is { } layer)
            draft.ResetFrom(layer);

        _drafts[channel] = draft;
        return draft;
    }

    private IReadOnlyDictionary<SquadObjectiveType, string>? FindObjectives()
    {
        if (CurrentLayer is { Objectives: { } current })
            return current;

        foreach (var layer in _layers)
        {
            if (layer.Objectives is { } objectives && objectives.Values.Any(o => !string.IsNullOrWhiteSpace(o)))
                return objectives;
        }

        return null;
    }

    private void OnFrame()
    {
        Refresh();
    }

    private void OnMapSelected(EntityUid map)
    {
        Refresh(true);
    }

    private void OnSubmitPressed()
    {
        if (_send == null || CurrentDraft is not { } draft || _drawTarget is not { } channel)
            return;

        var strokes = draft.Strokes
            .Select(s => s with { Points = new List<Vector2>(s.Points) })
            .ToList();

        var labels = new Dictionary<Vector2i, TacticalMapLabelData>(draft.Labels);
        _send(new TacticalMapSubmitCanvasMsg(_entMan.GetNetEntity(channel), strokes, labels));
        draft.MarkSubmitted();
        _view.ShowSubmitFeedback();
    }

    private void OnClearPressed()
    {
        if (CurrentDraft is not { } draft)
            return;

        draft.Clear();
        _view.RefreshDraft();
    }

    private void OnUndoPressed()
    {
        if (CurrentDraft is { } draft && draft.Undo())
            _view.RefreshDraft();
    }

    private void OnDrawTargetSelected(EntityUid channel)
    {
        if (_drawTarget == channel || !_layers.Any(l => l.Channel == channel && l.Drawable))
            return;

        _drawTarget = channel;
        PushCanvas();
    }

    private void OnLayerVisibilityToggled(EntityUid channel)
    {
        if (!_hiddenLayers.Remove(channel))
            _hiddenLayers.Add(channel);

        PushCanvas();
    }

    private void OnBlipActivated(NetEntity target)
    {
        _send?.Invoke(new TacticalMapActivateBlipMsg(target));
    }

    private void OnPointActivated(EntityUid map, Vector2i indices)
    {
        _send?.Invoke(new TacticalMapActivatePointMsg(_entMan.GetNetEntity(map), indices));
    }

    private EntityUid? GetPreferredMap()
    {
        if (_entMan.TryGetComponent(Viewer, out TacticalMapUserComponent? user) && user.SelfMap is { } selfMap)
            return selfMap;

        var source = _player.LocalEntity ?? Viewer;
        if (!_entMan.TryGetComponent(source, out TransformComponent? xform))
            return null;

        if (xform.GridUid is { } grid && _entMan.HasComponent<TacticalMapComponent>(grid))
            return grid;

        if (xform.MapUid is { } mapUid && _entMan.HasComponent<TacticalMapComponent>(mapUid))
            return mapUid;

        return null;
    }

    private bool IsPointMode()
    {
        if (_player.LocalEntity is not { } player)
            return false;

        if (_entMan.HasComponent<GhostComponent>(player))
            return true;

        return _entMan.TryGetComponent(player, out QueenEyeActionComponent? eye) && eye.Eye != null;
    }

    public void Dispose()
    {
        _view.Frame -= OnFrame;
        _view.MapSelected -= OnMapSelected;
        _view.SubmitPressed -= OnSubmitPressed;
        _view.ClearPressed -= OnClearPressed;
        _view.UndoPressed -= OnUndoPressed;
        _view.DrawTargetSelected -= OnDrawTargetSelected;
        _view.LayerVisibilityToggled -= OnLayerVisibilityToggled;
        _view.BlipActivated -= OnBlipActivated;
        _view.PointActivated -= OnPointActivated;
    }
}
