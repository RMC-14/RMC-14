using System.Linq;
using System.Numerics;
using Content.Client._RMC14.TacticalMap;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Xenonids.Construction.Tunnel;

public struct TunnelPathfindingConfig
{
    public double DirectDistanceWeight { get; set; }
    public double TunnelHopPenalty { get; set; }
    public double BacktrackingPenalty { get; set; }
    public double MaxConnectionDistance { get; set; }
    public int MaxIntermediateTunnels { get; set; }

    public static TunnelPathfindingConfig Default => new()
    {
        DirectDistanceWeight = 1.0,
        TunnelHopPenalty = 0.3,
        BacktrackingPenalty = 5.0,
        MaxConnectionDistance = 800.0,
        MaxIntermediateTunnels = 1,
    };
}

public struct TunnelCacheEntry
{
    public Vector2i Position;
    public string Name;
    public NetEntity Entity;
}

[UsedImplicitly]
public sealed class SelectDestinationTunnelBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly SpriteSpecifier.Rsi HighlightBackground =
        new(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "background");

    private static readonly Color PathColor = Color.FromHex("#F535AA");
    private const float PathThickness = 2f;
    private const float PathDashLength = 2f;
    private const float PathDashGap = 1.5f;

    private readonly TacticalMapSystem _tacticalMap;

    private SelectDestinationTunnelWindow? _window;
    private NetEntity? _selectedTunnel;
    private Dictionary<string, NetEntity> _availableTunnels = new();
    private readonly Dictionary<NetEntity, string> _tunnelNames = new();
    private NetEntity? _currentTunnel;
    private bool _showOnlyTunnels = true;
    private TunnelPathfindingConfig _pathfindingConfig = TunnelPathfindingConfig.Default;

    private readonly Dictionary<NetEntity, TunnelCacheEntry> _tunnelCache = new();
    private readonly Dictionary<(Vector2i, Vector2i), double> _distanceCache = new();
    private readonly Dictionary<(Vector2i, Vector2i), List<Vector2i>> _pathCache = new();
    private readonly List<EntityUid> _channels = new();
    private readonly List<TacticalMapMapOption> _maps = new();
    private readonly List<TacticalMapBlipView> _allBlips = new();
    private readonly List<TacticalMapBlipView> _shownBlips = new();

    private Vector2i? _cachedCurrentPos;
    private Vector2i? _cachedSelectedPos;

    public SelectDestinationTunnelBui(EntityUid ent, Enum key) : base(ent, key)
    {
        IoCManager.InjectDependencies(this);
        _tacticalMap = EntMan.System<TacticalMapSystem>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not SelectDestinationTunnelInterfaceState newState)
            return;

        Refresh(newState);
    }

    private void Refresh(SelectDestinationTunnelInterfaceState state)
    {
        var previouslySelectedTunnel = _selectedTunnel;
        var tunnelsChanged = !_availableTunnels.SequenceEqual(state.HiveTunnels);
        _availableTunnels = state.HiveTunnels;

        if (tunnelsChanged)
        {
            _tunnelNames.Clear();
            foreach (var (name, tunnel) in _availableTunnels)
            {
                _tunnelNames[tunnel] = name;
            }

            InvalidateCache();
        }

        if (_window == null)
        {
            Close();
            return;
        }

        _window.SelectableTunnels.Clear();

        UpdateTunnelList(state);
        UpdateSelectedTunnel(previouslySelectedTunnel);
        UpdateBlips();
    }

    private void InvalidateCache()
    {
        _tunnelCache.Clear();
        _distanceCache.Clear();
        _pathCache.Clear();
        _cachedCurrentPos = null;
        _cachedSelectedPos = null;
    }

    private void UpdateTunnelList(SelectDestinationTunnelInterfaceState newState)
    {
        if (_window == null)
            return;

        _currentTunnel = null;
        string? currentTunnelName = null;

        foreach (var tunnel in newState.HiveTunnels)
        {
            if (EntMan.GetEntity(tunnel.Value) == Owner)
            {
                currentTunnelName = tunnel.Key;
                _currentTunnel = tunnel.Value;
                continue;
            }

            _window.SelectableTunnels.Add(new ItemList.Item(_window.SelectableTunnels)
            {
                Text = tunnel.Key,
                Metadata = tunnel.Value
            });
        }

        _window.UpdateCurrentTunnelDisplay(currentTunnelName);
    }

    private void UpdateSelectedTunnel(NetEntity? previouslySelectedTunnel)
    {
        if (_window == null)
            return;

        if (previouslySelectedTunnel != null && _tunnelNames.TryGetValue(previouslySelectedTunnel.Value, out var name))
        {
            _selectedTunnel = previouslySelectedTunnel;
            _window.SelectButton.Disabled = false;
            _window.UpdateSelectedTunnelDisplay(name);
        }
        else
        {
            _selectedTunnel = null;
            _window.SelectButton.Disabled = true;
            _window.UpdateSelectedTunnelDisplay(null);
        }
    }

    private EntityUid? GetMap()
    {
        if (EntMan.TryGetComponent(Owner, out TransformComponent? xform))
        {
            if (xform.GridUid is { } grid && EntMan.HasComponent<TacticalMapComponent>(grid))
                return grid;

            if (xform.MapUid is { } map && EntMan.HasComponent<TacticalMapComponent>(map))
                return map;
        }

        _tacticalMap.CollectMaps(_channels, _maps);
        return _maps.Count > 0 ? _maps[0].Map : null;
    }

    private void UpdateBlips()
    {
        if (_window == null)
            return;

        var control = _window.TacticalMap;
        _allBlips.Clear();

        EntityUid? map = null;
        if (_player.LocalEntity is { } player && _tacticalMap.TryGetViewerChannels(player, _channels, out var live))
        {
            map = GetMap();
            if (map != null)
                _tacticalMap.CollectBlips(_channels, live, map.Value, _allBlips);
        }

        control.SetBaseMap(map);
        BuildTunnelCache();

        _shownBlips.Clear();
        Vector2i? currentTunnelPosition = null;
        Vector2i? selectedTunnelPosition = null;
        foreach (var view in _allBlips)
        {
            if (_showOnlyTunnels && !_tunnelNames.ContainsKey(view.Entity))
                continue;

            var blip = view.Blip;
            if (view.Entity == _currentTunnel)
            {
                currentTunnelPosition ??= blip.Indices;
                blip = HighlightBlip(blip);
            }
            else if (view.Entity == _selectedTunnel)
            {
                selectedTunnelPosition ??= blip.Indices;
                blip = HighlightBlip(blip);
            }

            _shownBlips.Add(new TacticalMapBlipView(view.Entity, blip));
        }

        control.SetBlips(_shownBlips);

        if (currentTunnelPosition != _cachedCurrentPos || selectedTunnelPosition != _cachedSelectedPos)
        {
            UpdateDirectionalArrow(currentTunnelPosition, selectedTunnelPosition);
            _cachedCurrentPos = currentTunnelPosition;
            _cachedSelectedPos = selectedTunnelPosition;
        }
    }

    private void BuildTunnelCache()
    {
        _tunnelCache.Clear();
        foreach (var view in _allBlips)
        {
            if (!_tunnelNames.TryGetValue(view.Entity, out var name))
                continue;

            _tunnelCache[view.Entity] = new TunnelCacheEntry
            {
                Position = view.Blip.Indices,
                Name = name,
                Entity = view.Entity,
            };
        }
    }

    private static TacticalMapBlip HighlightBlip(TacticalMapBlip blip)
    {
        return blip with
        {
            Background = HighlightBackground,
            Flags = blip.Flags | TacticalMapBlipFlags.HiveLeader,
        };
    }

    private List<Vector2i> GetAllTunnelPositions()
    {
        return _tunnelCache.Values.Select(entry => entry.Position).ToList();
    }

    private List<Vector2i> FindTunnelPath(Vector2i startPos, Vector2i endPos)
    {
        var cacheKey = (startPos, endPos);
        if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
            return cachedPath;

        var allTunnelPositions = GetAllTunnelPositions();
        var tunnelsOnPath = GetTunnelsOnDirectLine(startPos, endPos, allTunnelPositions);
        var result = BuildOptimalPath(startPos, endPos, tunnelsOnPath);

        _pathCache[cacheKey] = result;
        return result;
    }

    private List<Vector2i> GetTunnelsOnDirectLine(Vector2i start, Vector2i end, List<Vector2i> allPositions)
    {
        const double maxDistanceFromLine = 30.0;

        var tunnelsOnLine = allPositions
            .Where(pos => pos != start && pos != end)
            .Where(pos => IsDirectlyOnPath(start, end, pos, maxDistanceFromLine))
            .OrderBy(pos => CalculateProgressAlongPath(start, end, pos))
            .ToList();

        return tunnelsOnLine;
    }

    private bool IsDirectlyOnPath(Vector2i start, Vector2i end, Vector2i tunnel, double maxDistance)
    {
        var distanceFromLine = CalculateDistanceFromLine(start, end, tunnel);
        if (distanceFromLine > maxDistance)
            return false;

        var minDistanceFromEndpoints = 50.0;
        var distanceFromStart = CalculateDistance(start, tunnel);
        var distanceFromEnd = CalculateDistance(end, tunnel);

        if (distanceFromStart < minDistanceFromEndpoints || distanceFromEnd < minDistanceFromEndpoints)
            return false;

        var progress = CalculateProgressAlongPath(start, end, tunnel);
        return progress >= 0.15 && progress <= 0.85;
    }

    private List<Vector2i> BuildOptimalPath(Vector2i start, Vector2i end, List<Vector2i> tunnelsOnLine)
    {
        var path = new List<Vector2i> { start };
        var addedIntermediateTunnels = 0;

        if (_pathfindingConfig.MaxIntermediateTunnels <= 0)
        {
            path.Add(end);
            return path;
        }

        var tunnelCosts = tunnelsOnLine.Select(tunnel => new
            {
                Position = tunnel,
                Cost = CalculateTunnelCost(path.Last(), tunnel, end)
            })
            .OrderBy(t => t.Cost)
            .ToList();

        foreach (var tunnelInfo in tunnelCosts)
        {
            if (addedIntermediateTunnels >= _pathfindingConfig.MaxIntermediateTunnels)
                break;

            var distanceFromLast = CalculateDistance(path.Last(), tunnelInfo.Position);
            if (distanceFromLast <= _pathfindingConfig.MaxConnectionDistance)
            {
                path.Add(tunnelInfo.Position);
                addedIntermediateTunnels++;
            }
        }

        path.Add(end);
        return path;
    }

    private double CalculateTunnelCost(Vector2i current, Vector2i tunnel, Vector2i destination)
    {
        var distanceToTunnel = CalculateDistance(current, tunnel) * _pathfindingConfig.DirectDistanceWeight;
        var distanceFromTunnelToDestination = CalculateDistance(tunnel, destination) * _pathfindingConfig.DirectDistanceWeight;
        var hopPenalty = _pathfindingConfig.TunnelHopPenalty;

        var directDistance = CalculateDistance(current, destination);
        var routeDistance = distanceToTunnel + distanceFromTunnelToDestination;
        var distanceSavings = Math.Max(0, directDistance - routeDistance);

        return routeDistance + hopPenalty - distanceSavings;
    }

    private double CalculateDistanceFromLine(Vector2i start, Vector2i end, Vector2i point)
    {
        var lineVec = new Vector2(end.X - start.X, end.Y - start.Y);
        var pointVec = new Vector2(point.X - start.X, point.Y - start.Y);

        var lineLength = lineVec.Length();
        if (lineLength < 0.001)
            return Vector2.Distance(new Vector2(start.X, start.Y), new Vector2(point.X, point.Y));

        var lineUnit = Vector2.Normalize(lineVec);
        var projection = Vector2.Dot(pointVec, lineUnit);
        var projectedPoint = lineUnit * projection;

        return Vector2.Distance(pointVec, projectedPoint);
    }

    private double CalculateProgressAlongPath(Vector2i start, Vector2i end, Vector2i point)
    {
        var totalVec = new Vector2(end.X - start.X, end.Y - start.Y);
        var pointVec = new Vector2(point.X - start.X, point.Y - start.Y);

        var totalLengthSquared = totalVec.LengthSquared();
        if (totalLengthSquared < 0.001)
            return 0;

        var dotProduct = Vector2.Dot(pointVec, totalVec);
        return dotProduct / totalLengthSquared;
    }

    private double CalculateDistance(Vector2i pos1, Vector2i pos2)
    {
        var key = (pos1, pos2);
        if (_distanceCache.TryGetValue(key, out var cached))
            return cached;

        var result = Math.Sqrt(Math.Pow(pos2.X - pos1.X, 2) + Math.Pow(pos2.Y - pos1.Y, 2));
        _distanceCache[key] = result;
        return result;
    }

    private void UpdateDirectionalArrow(Vector2i? currentPosition, Vector2i? selectedPosition)
    {
        if (_window == null)
            return;

        var strokes = _window.TacticalMap.OverlayStrokes;
        strokes.Clear();

        if (currentPosition is not { } current ||
            selectedPosition is not { } selected ||
            current == selected)
        {
            return;
        }

        var path = FindTunnelPath(current, selected);
        for (var i = 0; i < path.Count - 1; i++)
        {
            AddDashedSegment(strokes, path[i], path[i + 1]);
        }
    }

    private static void AddDashedSegment(List<TacticalMapStroke> strokes, Vector2i from, Vector2i to)
    {
        var start = new Vector2(from.X + 0.5f, from.Y + 0.5f);
        var end = new Vector2(to.X + 0.5f, to.Y + 0.5f);
        var delta = end - start;
        var length = delta.Length();
        if (length < 0.001f)
            return;

        var direction = delta / length;
        for (var position = 0f; position < length; position += PathDashLength + PathDashGap)
        {
            var dashEnd = MathF.Min(position + PathDashLength, length);
            strokes.Add(new TacticalMapStroke(
                PathColor,
                PathThickness,
                false,
                new List<Vector2> { start + direction * position, start + direction * dashEnd }));
        }
    }

    private void OnBlipClicked(NetEntity entity)
    {
        if (_window == null ||
            entity == _currentTunnel ||
            !_tunnelNames.TryGetValue(entity, out var tunnelName))
        {
            return;
        }

        _selectedTunnel = entity;
        _window.SelectButton.Disabled = false;
        _window.UpdateSelectedTunnelDisplay(tunnelName);
        UpdateBlips();
    }

    private void OnBlipRightClicked(NetEntity entity, Vector2i indices)
    {
        if (_window == null || !_tunnelNames.TryGetValue(entity, out var tunnelName))
            return;

        var control = _window.TacticalMap;
        var text = Loc.GetString("ui-tactical-map-tunnel-info", ("name", tunnelName));
        if (control.InfoIndices == indices && control.InfoText == text)
        {
            control.InfoText = null;
            control.InfoIndices = null;
            return;
        }

        control.InfoText = text;
        control.InfoIndices = indices;
    }

    public void ConfigurePathfinding(TunnelPathfindingConfig config)
    {
        _pathfindingConfig = config;
        _distanceCache.Clear();
        _pathCache.Clear();
    }

    protected override void Open()
    {
        base.Open();

        if (_window is { IsOpen: true })
            return;

        _window = this.CreateWindow<SelectDestinationTunnelWindow>();
        _window.SelectButton.Disabled = true;
        _window.SetBlipUpdateCallback(UpdateBlips);

        _window.ShowOnlyTunnelsCheckbox.OnPressed += args =>
        {
            _showOnlyTunnels = args.Button.Pressed;
            UpdateBlips();
        };

        _window.SelectableTunnels.OnItemSelected += args =>
        {
            _window.SelectButton.Disabled = false;
            _selectedTunnel = (NetEntity) args.ItemList[args.ItemIndex].Metadata!;
            _window.UpdateSelectedTunnelDisplay(_tunnelNames.GetValueOrDefault(_selectedTunnel.Value));
            UpdateBlips();
        };

        _window.SelectButton.OnButtonDown += args =>
        {
            if (_selectedTunnel is null)
            {
                args.Button.Disabled = true;
                return;
            }

            GoToTunnel(_selectedTunnel.Value);
            Close();
        };

        _window.TacticalMap.BlipClicked += OnBlipClicked;
        _window.TacticalMap.BlipRightClicked += OnBlipRightClicked;
    }

    private void GoToTunnel(NetEntity destinationTunnel)
    {
        SendMessage(new TraverseXenoTunnelMessage(destinationTunnel));
    }
}
