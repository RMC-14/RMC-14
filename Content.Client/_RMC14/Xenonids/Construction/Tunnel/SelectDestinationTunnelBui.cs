using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Control;

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
    public int EntityId;
}

[UsedImplicitly]
public sealed class SelectDestinationTunnelBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private SelectDestinationTunnelWindow? _window;
    private NetEntity? _selectedTunnel;
    private Dictionary<string, NetEntity> _availableTunnels = new();
    private int? _currentTunnelNetEntityKey;
    private bool _showOnlyTunnels = true;
    private TunnelPathfindingConfig _pathfindingConfig = TunnelPathfindingConfig.Default;

    private readonly Dictionary<int, TunnelCacheEntry> _tunnelCache = new();
    private readonly Dictionary<Vector2i, int> _positionToEntityCache = new();
    private readonly Dictionary<(Vector2i, Vector2i), double> _distanceCache = new();
    private readonly Dictionary<(Vector2i, Vector2i), List<Vector2i>> _pathCache = new();
    private readonly List<TacticalMapBlip> _reusableBlipsList = new();

    private bool _cacheValid;
    private Vector2i? _cachedCurrentPos;
    private Vector2i? _cachedSelectedPos;

    public SelectDestinationTunnelBui(EntityUid ent, Enum key) : base(ent, key)
    {
        IoCManager.InjectDependencies(this);
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
        UpdateTacticalMapDisplay();
        UpdateBlips();
    }

    private void InvalidateCache()
    {
        _tunnelCache.Clear();
        _positionToEntityCache.Clear();
        _distanceCache.Clear();
        _pathCache.Clear();
        _cacheValid = false;
        _cachedCurrentPos = null;
        _cachedSelectedPos = null;
    }

    private void UpdateTunnelList(SelectDestinationTunnelInterfaceState newState)
    {
        if (_window == null)
            return;

        _currentTunnelNetEntityKey = null;
        string? currentTunnelName = null;

        foreach (var tunnel in newState.HiveTunnels)
        {
            if (EntMan.GetEntity(tunnel.Value) == Owner)
            {
                currentTunnelName = tunnel.Key;
                _currentTunnelNetEntityKey = (int)tunnel.Value;
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

        if (previouslySelectedTunnel != null && _availableTunnels.ContainsValue(previouslySelectedTunnel.Value))
        {
            _selectedTunnel = previouslySelectedTunnel;
            _window.SelectButton.Disabled = false;
            _window.UpdateSelectedTunnelDisplay(GetTunnelNameCached(_selectedTunnel.Value));
        }
        else
        {
            _selectedTunnel = null;
            _window.SelectButton.Disabled = true;
            _window.UpdateSelectedTunnelDisplay(null);
        }
    }

    private string? GetTunnelNameCached(NetEntity tunnel)
    {
        var entityId = (int)tunnel;
        return _tunnelCache.TryGetValue(entityId, out var cached)
            ? cached.Name
            : GetTunnelName(tunnel);
    }

    private string? GetTunnelName(NetEntity tunnel)
    {
        foreach (var kvp in _availableTunnels)
        {
            if (kvp.Value == tunnel)
                return kvp.Key;
        }
        return null;
    }

    private void UpdateTacticalMapDisplay()
    {
        if (_window == null)
            return;

        if (_player.LocalEntity is { } player &&
            EntMan.TryGetComponent(player, out TacticalMapUserComponent? user) &&
            EntMan.TryGetComponent(user.Map, out AreaGridComponent? areaGrid))
        {
            _window.TacticalMapWrapper.UpdateTexture((user.Map.Value, areaGrid));
        }
    }

    private void BuildTunnelCache(TacticalMapUserComponent user)
    {
        if (_cacheValid)
            return;

        _tunnelCache.Clear();
        _positionToEntityCache.Clear();

        var blipCollections = new[]
        {
            user.XenoStructureBlips,
            user.XenoBlips,
            user.MarineBlips
        };

        foreach (var blipCollection in blipCollections)
        {
            foreach (var kvp in blipCollection)
            {
                var entityId = kvp.Key;
                var blip = kvp.Value;

                var tunnelName = GetTunnelNameByEntityId(entityId);
                if (tunnelName != null && _availableTunnels.ContainsKey(tunnelName))
                {
                    var cacheEntry = new TunnelCacheEntry
                    {
                        Position = blip.Indices,
                        Name = tunnelName,
                        Entity = _availableTunnels[tunnelName],
                        EntityId = entityId
                    };

                    _tunnelCache[entityId] = cacheEntry;
                    _positionToEntityCache[blip.Indices] = entityId;
                }
            }
        }

        _cacheValid = true;
    }

    private void UpdateBlips()
    {
        if (_window == null || _player.LocalEntity is not { } player ||
            !EntMan.TryGetComponent(player, out TacticalMapUserComponent? user))
        {
            _window?.TacticalMapWrapper.UpdateBlips(null);
            return;
        }

        BuildTunnelCache(user);

        _reusableBlipsList.Clear();

        var (currentTunnelPosition, selectedTunnelPosition) = ProcessBlipCollections(user,
            _selectedTunnel != null ? (int)_selectedTunnel.Value : null,
            _reusableBlipsList);

        _window.TacticalMapWrapper.UpdateBlips(_reusableBlipsList.ToArray());

        if (currentTunnelPosition != _cachedCurrentPos || selectedTunnelPosition != _cachedSelectedPos)
        {
            UpdateDirectionalArrow(currentTunnelPosition, selectedTunnelPosition);
            _cachedCurrentPos = currentTunnelPosition;
            _cachedSelectedPos = selectedTunnelPosition;
        }
    }

    private void GetTunnelEntityIds(int? selectedTunnelKey, HashSet<int> output)
    {
        if (_currentTunnelNetEntityKey.HasValue)
            output.Add(_currentTunnelNetEntityKey.Value);

        foreach (var netTunnel in _availableTunnels.Values)
        {
            output.Add((int)netTunnel);
        }

        if (selectedTunnelKey.HasValue)
            output.Add(selectedTunnelKey.Value);
    }

    private (Vector2i? currentPos, Vector2i? selectedPos) ProcessBlipCollections(TacticalMapUserComponent user,
        int? selectedTunnelKey,
        List<TacticalMapBlip> blipsList)
    {
        Vector2i? currentTunnelPosition = null;
        Vector2i? selectedTunnelPosition = null;

        var tunnelEntityIds = new HashSet<int>(_availableTunnels.Values.Select(t => (int)t));
        if (_currentTunnelNetEntityKey.HasValue)
            tunnelEntityIds.Add(_currentTunnelNetEntityKey.Value);
        if (selectedTunnelKey.HasValue)
            tunnelEntityIds.Add(selectedTunnelKey.Value);

        var blipCollections = new[]
        {
            user.XenoStructureBlips,
            user.XenoBlips,
            user.MarineBlips
        };

        foreach (var blips in blipCollections)
        {
            foreach (var (entityId, blip) in blips)
            {
                if (_showOnlyTunnels && !tunnelEntityIds.Contains(entityId))
                    continue;

                blipsList.Add(HighlightBlip(blip, entityId, selectedTunnelKey));

                if (_currentTunnelNetEntityKey == entityId && currentTunnelPosition == null)
                    currentTunnelPosition = blip.Indices;

                if (selectedTunnelKey == entityId && selectedTunnelPosition == null)
                    selectedTunnelPosition = blip.Indices;
            }
        }

        return (currentTunnelPosition, selectedTunnelPosition);
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

    private string? GetTunnelNameByEntityId(int entityId)
    {
        if (_tunnelCache.TryGetValue(entityId, out var cached))
            return cached.Name;

        foreach (var kvp in _availableTunnels)
        {
            if ((int)kvp.Value == entityId)
                return kvp.Key;
        }
        return null;
    }

    private void UpdateDirectionalArrow(Vector2i? currentPosition, Vector2i? selectedPosition)
    {
        if (_window == null)
            return;

        _window.TacticalMapWrapper.Map.RemoveLinesByColor(Color.FromHex("#F535AA"));

        if (currentPosition.HasValue && selectedPosition.HasValue && currentPosition != selectedPosition)
        {
            _window.TacticalMapWrapper.Map.AddDashedTunnelPathClosest(
                FindTunnelPath(currentPosition.Value, selectedPosition.Value),
                Color.FromHex("#F535AA"));
        }
    }

    private TacticalMapBlip HighlightBlip(TacticalMapBlip blip, int entityId, int? selectedTunnelKey)
    {
        if (_currentTunnelNetEntityKey.HasValue && entityId == _currentTunnelNetEntityKey.Value)
        {
            return blip with
            {
                Background = new SpriteSpecifier.Rsi(new("_RMC14/Interface/map_blips.rsi"), "background"),
                HiveLeader = true
            };
        }

        if (selectedTunnelKey.HasValue && entityId == selectedTunnelKey.Value)
        {
            return blip with
            {
                Background = new SpriteSpecifier.Rsi(new("_RMC14/Interface/map_blips.rsi"), "background"),
                HiveLeader = true
            };
        }

        return blip;
    }

    private void OnBlipClicked(Vector2i clickedIndices)
    {
        if (_window == null)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        if (!EntMan.TryGetComponent(player, out TacticalMapUserComponent? user))
            return;

        var foundEntityId = _positionToEntityCache.TryGetValue(clickedIndices, out var entityId)
            ? entityId
            : FindEntityIdAtIndices(clickedIndices, user);

        if (!foundEntityId.HasValue)
            return;

        var tunnelName = GetTunnelNameByEntityId(foundEntityId.Value);
        if (tunnelName == null || !_availableTunnels.TryGetValue(tunnelName, out var value))
            return;

        if (_currentTunnelNetEntityKey.HasValue && foundEntityId.Value == _currentTunnelNetEntityKey.Value)
            return;

        _selectedTunnel = value;
        _window.SelectButton.Disabled = false;
        _window.UpdateSelectedTunnelDisplay(tunnelName);
        UpdateBlips();
    }

    private void OnBlipRightClicked(Vector2i clickedIndices, string _)
    {
        if (_window == null)
            return;

        if (_player.LocalEntity is not { } player ||
            !EntMan.TryGetComponent(player, out TacticalMapUserComponent? user))
        {
            return;
        }

        var foundEntityId = _positionToEntityCache.TryGetValue(clickedIndices, out var entityId)
            ? entityId
            : FindEntityIdAtIndices(clickedIndices, user);

        if (!foundEntityId.HasValue)
            return;

        var tunnelName = GetTunnelNameByEntityId(foundEntityId.Value);
        if (tunnelName == null || !_availableTunnels.ContainsKey(tunnelName))
            return;

        var screenPos = _window.TacticalMapWrapper.Map.IndicesToPosition(clickedIndices);
        _window.TacticalMapWrapper.Map.ShowTunnelInfo(clickedIndices, tunnelName, screenPos);
        _window.TacticalMapWrapper.Canvas.ShowTunnelInfo(clickedIndices, tunnelName, screenPos);
    }

    private int? FindEntityIdAtIndices(Vector2i indices, TacticalMapUserComponent user)
    {
        foreach (var kvp in user.XenoStructureBlips)
        {
            if (kvp.Value.Indices == indices)
                return kvp.Key;
        }

        foreach (var kvp in user.XenoBlips)
        {
            if (kvp.Value.Indices == indices)
                return kvp.Key;
        }

        foreach (var kvp in user.MarineBlips)
        {
            if (kvp.Value.Indices == indices)
                return kvp.Key;
        }

        return null;
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
        _window.SetBlipUpdateCallback(() => UpdateBlips());

        var wrapper = _window.TacticalMapWrapper;
        TabContainer.SetTabVisible(wrapper.CanvasTab, false);
        wrapper.Tabs.CurrentTab = 0;

        wrapper.Map.MouseFilter = MouseFilterMode.Stop;
        wrapper.Canvas.MouseFilter = MouseFilterMode.Stop;

        _window.ShowOnlyTunnelsCheckbox.OnPressed += args =>
        {
            _showOnlyTunnels = args.Button.Pressed;
            UpdateBlips();
        };

        _window.SelectableTunnels.OnItemSelected += args =>
        {
            _window.SelectButton.Disabled = false;
            _selectedTunnel = (NetEntity)args.ItemList[args.ItemIndex].Metadata!;
            _window.UpdateSelectedTunnelDisplay(GetTunnelNameCached(_selectedTunnel.Value));
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

        _window.TacticalMapWrapper.Map.OnBlipClicked = OnBlipClicked;
        _window.TacticalMapWrapper.Canvas.OnBlipClicked = OnBlipClicked;
        _window.TacticalMapWrapper.Map.OnBlipRightClicked = OnBlipRightClicked;
        _window.TacticalMapWrapper.Canvas.OnBlipRightClicked = OnBlipRightClicked;
    }

    private void GoToTunnel(NetEntity destinationTunnel)
    {
        SendMessage(new TraverseXenoTunnelMessage(destinationTunnel));
    }
}
