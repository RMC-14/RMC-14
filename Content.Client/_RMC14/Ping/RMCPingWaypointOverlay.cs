using Content.Client.Resources;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Client._RMC14.Ping;

public abstract class RMCPingWaypointOverlay : Overlay
{
    [Dependency] protected readonly IEntityManager Entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly TransformSystem _transform;
    private readonly ShaderInstance _shader;
    private readonly Font _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const float EdgePadding = 120f;
    private const float BaseWaypointScale = 1.2f;
    private const float PulseAmplitude = 0.08f;
    private const float PulseFrequency = 1.8f;
    private const float FadeInDistance = 300f;
    private const float MaxVisibleDistance = 2000f;
    private const float MaxVisibleDistanceSquared = MaxVisibleDistance * MaxVisibleDistance;
    private const float ScreenMargin = 50f;

    private const float WaypointRadius = 22f;
    private const float MinSeparationDistance = 5f;
    private const int MaxOverlapIterations = 4;
    private const float GroupingDistance = 180f;
    private const float GroupingDistanceSquared = GroupingDistance * GroupingDistance;
    private const float MinGroupingViewDistance = 25f;
    private const float MinGroupingViewDistanceSquared = MinGroupingViewDistance * MinGroupingViewDistance;
    private const float RepulsionForce = 10f;
    private const float Damping = 0.85f;
    private const float MaxVelocity = 5f;

    private const double ScreenSizeCheckInterval = 0.3;
    private const double PositionCacheTimeout = 0.8;

    private Vector2 _lastScreenSize = Vector2.Zero;
    private TimeSpan _lastScreenSizeCheck = TimeSpan.Zero;
    private Vector2 _screenCenter;
    private WaypointBounds _screenBounds;
    private readonly Dictionary<EntityUid, CachedWaypointData> _waypointCache = new();
    private readonly List<WaypointPositionData> _workingPositions = new();
    private readonly HashSet<EntityUid> _expiredEntities = new();
    private readonly Dictionary<EntityUid, Vector2> _velocities = new();
    private readonly List<PingWaypointData> _filteredWaypoints = new();
    private readonly List<PingWaypointData> _groupedWaypoints = new();
    private readonly HashSet<EntityUid> _groupedProcessed = new();
    private readonly List<PingWaypointData> _groupScratch = new();
    private readonly float _fadeRange;

    protected RMCPingWaypointOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = Entity.System<TransformSystem>();
        _shader = _prototype.Index<ShaderPrototype>("unshaded").Instance();
        _font = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 9);
        _fadeRange = MaxVisibleDistance - FadeInDistance;
        ZIndex = 100;
    }

    protected abstract IReadOnlyDictionary<EntityUid, PingWaypointData> GetWaypoints();
    protected abstract bool CanViewWaypoints(EntityUid player);
    protected abstract bool ShouldIncludeWaypoint(PingWaypointData waypoint, EntityUid player);
    protected abstract Color GetCreatorTextColor(EntityUid creator);

    protected virtual int GetWaypointPriority(PingWaypointData waypoint) => 0;

    protected virtual float GetWaypointRadius(PingWaypointData waypoint)
    {
        var radius = WaypointRadius;
        if (waypoint.GroupCount > 1)
            radius *= 1.2f;

        return radius;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_players.LocalEntity is not { } player || !CanViewWaypoints(player))
            return;

        var handle = args.ScreenHandle;
        var screenSize = CheckAndUpdateScreenSize(args);

        handle.UseShader(_shader);

        var playerPos = _transform.GetWorldPosition(player);
        var waypoints = GetWaypoints();

        CleanExpiredEntities(waypoints);

        FilterWaypoints(waypoints, player, _filteredWaypoints);
        GroupWaypoints(_filteredWaypoints, playerPos, _groupedWaypoints);
        CalculateAndResolvePositions(_groupedWaypoints, playerPos, screenSize);

        foreach (var positionData in _workingPositions)
        {
            DrawWaypoint(handle, positionData);
        }

        handle.UseShader(null);
        _workingPositions.Clear();
        _filteredWaypoints.Clear();
        _groupedWaypoints.Clear();
    }

    private void FilterWaypoints(
        IReadOnlyDictionary<EntityUid, PingWaypointData> waypoints,
        EntityUid player,
        List<PingWaypointData> result)
    {
        result.Clear();
        foreach (var waypoint in waypoints.Values)
        {
            if (ShouldIncludeWaypoint(waypoint, player))
                result.Add(waypoint);
        }
    }

    private void CalculateAndResolvePositions(List<PingWaypointData> waypoints, Vector2 playerPos, Vector2 screenSize)
    {
        var currentTime = _timing.CurTime;

        foreach (var waypoint in waypoints)
        {
            if (!waypoint.IsValid || waypoint.Texture == null)
                continue;

            var distanceSquared = Vector2.DistanceSquared(waypoint.WorldPosition, playerPos);
            if (distanceSquared > MaxVisibleDistanceSquared)
                continue;

            if (!ShouldDrawWaypoint(waypoint, screenSize))
                continue;

            var distance = MathF.Sqrt(distanceSquared);
            var targetPosition = CalculateTargetPosition(waypoint.WorldPosition, playerPos);

            Vector2 currentPosition;
            if (_waypointCache.TryGetValue(waypoint.EntityUid, out var cached) &&
                (currentTime - cached.LastUpdate).TotalSeconds < PositionCacheTimeout)
            {
                currentPosition = cached.Position;
            }
            else
            {
                currentPosition = targetPosition;
                _waypointCache[waypoint.EntityUid] = new CachedWaypointData
                {
                    Position = currentPosition,
                    LastUpdate = currentTime
                };
            }

            _workingPositions.Add(new WaypointPositionData
            {
                Waypoint = waypoint,
                Position = currentPosition,
                TargetPosition = targetPosition,
                Distance = distance,
                Priority = GetWaypointPriority(waypoint),
                Radius = GetWaypointRadius(waypoint)
            });
        }

        ResolveCollisionsWithForces(currentTime);
    }

    private void ResolveCollisionsWithForces(TimeSpan currentTime)
    {
        const float deltaTime = 1f / 60f;

        _workingPositions.Sort((a, b) =>
        {
            var priorityComp = a.Priority.CompareTo(b.Priority);
            if (priorityComp != 0)
                return priorityComp;

            return a.Distance.CompareTo(b.Distance);
        });

        for (var iteration = 0; iteration < MaxOverlapIterations; iteration++)
        {
            var hasSignificantMovement = false;

            for (var i = 0; i < _workingPositions.Count; i++)
            {
                var current = _workingPositions[i];
                var uid = current.Waypoint.EntityUid;

                if (!_velocities.ContainsKey(uid))
                    _velocities[uid] = Vector2.Zero;

                var force = Vector2.Zero;
                force += (current.TargetPosition - current.Position) * 0.3f;

                for (var j = 0; j < _workingPositions.Count; j++)
                {
                    if (i == j)
                        continue;

                    var other = _workingPositions[j];
                    var separation = current.Position - other.Position;
                    var distance = separation.Length();
                    var minDistance = current.Radius + other.Radius + MinSeparationDistance;

                    if (distance < minDistance && distance > 0.001f)
                    {
                        var overlap = minDistance - distance;
                        var repulsion = separation / distance * overlap * RepulsionForce;
                        if (current.Priority > other.Priority)
                            repulsion *= 1.5f;

                        force += repulsion;
                    }
                }

                force += CalculateBoundaryForces(current.Position, current.Radius);

                _velocities[uid] = (_velocities[uid] + force * deltaTime) * Damping;
                if (_velocities[uid].LengthSquared() > MaxVelocity * MaxVelocity)
                    _velocities[uid] = Vector2.Normalize(_velocities[uid]) * MaxVelocity;

                var newPosition = current.Position + _velocities[uid] * deltaTime * 3f;
                newPosition = ClampToScreenBounds(newPosition, current.Radius);
                if (Vector2.Distance(current.Position, newPosition) > 0.2f)
                    hasSignificantMovement = true;

                _workingPositions[i] = current with { Position = newPosition };
                _waypointCache[uid] = new CachedWaypointData
                {
                    Position = newPosition,
                    LastUpdate = currentTime
                };
            }

            if (!hasSignificantMovement)
                break;
        }
    }

    private Vector2 CalculateBoundaryForces(Vector2 position, float radius)
    {
        var force = Vector2.Zero;
        const float pushForce = 25f;

        if (position.X - radius < _screenBounds.Left)
            force.X += pushForce;
        if (position.X + radius > _screenBounds.Right)
            force.X -= pushForce;
        if (position.Y - radius < _screenBounds.Top)
            force.Y += pushForce;
        if (position.Y + radius > _screenBounds.Bottom)
            force.Y -= pushForce;

        return force;
    }

    private Vector2 ClampToScreenBounds(Vector2 position, float radius)
    {
        return new Vector2(
            Math.Clamp(position.X, _screenBounds.Left + radius, _screenBounds.Right - radius),
            Math.Clamp(position.Y, _screenBounds.Top + radius, _screenBounds.Bottom - radius));
    }

    private void CleanExpiredEntities(IReadOnlyDictionary<EntityUid, PingWaypointData> waypoints)
    {
        _expiredEntities.Clear();

        foreach (var uid in _waypointCache.Keys)
        {
            if (!waypoints.ContainsKey(uid))
                _expiredEntities.Add(uid);
        }

        foreach (var uid in _expiredEntities)
        {
            _waypointCache.Remove(uid);
            _velocities.Remove(uid);
        }
    }

    private void GroupWaypoints(List<PingWaypointData> waypoints, Vector2 playerPos, List<PingWaypointData> result)
    {
        result.Clear();
        _groupedProcessed.Clear();

        foreach (var waypoint in waypoints)
        {
            if (!_groupedProcessed.Add(waypoint.EntityUid))
                continue;

            var playerDistanceSquared = Vector2.DistanceSquared(waypoint.WorldPosition, playerPos);
            if (playerDistanceSquared < MinGroupingViewDistanceSquared)
            {
                result.Add(waypoint);
                continue;
            }

            _groupScratch.Clear();
            _groupScratch.Add(waypoint);

            foreach (var otherWaypoint in waypoints)
            {
                if (_groupedProcessed.Contains(otherWaypoint.EntityUid))
                    continue;
                if (otherWaypoint.Creator != waypoint.Creator)
                    continue;
                if (otherWaypoint.PingType != waypoint.PingType)
                    continue;

                var distanceSquared = Vector2.DistanceSquared(waypoint.WorldPosition, otherWaypoint.WorldPosition);
                if (distanceSquared <= GroupingDistanceSquared)
                {
                    _groupScratch.Add(otherWaypoint);
                    _groupedProcessed.Add(otherWaypoint.EntityUid);
                }
            }

            result.Add(_groupScratch.Count > 1 ? CreateGroupedWaypoint(_groupScratch) : waypoint);
        }
    }

    private PingWaypointData CreateGroupedWaypoint(List<PingWaypointData> group)
    {
        var totalWeight = 0f;
        var weightedCenter = Vector2.Zero;
        var currentTime = _timing.CurTime;
        var baseWaypoint = group[0];
        var maxDeleteAt = baseWaypoint.DeleteAt;
        var groupedUid = baseWaypoint.EntityUid;

        foreach (var waypoint in group)
        {
            var remainingLifetime = (float) Math.Max(0, (waypoint.DeleteAt - currentTime).TotalSeconds);
            var weight = Math.Max(0.1f, remainingLifetime + 0.1f);
            weightedCenter += waypoint.WorldPosition * weight;
            totalWeight += weight;

            if (waypoint.DeleteAt > maxDeleteAt)
            {
                maxDeleteAt = waypoint.DeleteAt;
                baseWaypoint = waypoint;
            }

            if (waypoint.EntityUid.Id < groupedUid.Id)
                groupedUid = waypoint.EntityUid;
        }

        weightedCenter /= totalWeight;

        return new PingWaypointData(
            groupedUid,
            baseWaypoint.PingType,
            baseWaypoint.Creator,
            weightedCenter,
            baseWaypoint.OriginalCoordinates,
            baseWaypoint.MapId,
            baseWaypoint.Color,
            baseWaypoint.Texture,
            maxDeleteAt)
        {
            EntityIsLoaded = baseWaypoint.EntityIsLoaded,
            GroupCount = group.Count
        };
    }

    private Vector2 CheckAndUpdateScreenSize(in OverlayDrawArgs args)
    {
        var currentTime = _timing.CurTime;

        if (currentTime - _lastScreenSizeCheck >= TimeSpan.FromSeconds(ScreenSizeCheckInterval))
        {
            _lastScreenSizeCheck = currentTime;
            var gameViewportSize = CalculateGameViewportSize(args);

            if (Vector2.Distance(gameViewportSize, _lastScreenSize) > 10f)
            {
                _lastScreenSize = gameViewportSize;
                _screenCenter = gameViewportSize * 0.5f;
                _screenBounds = new WaypointBounds(gameViewportSize, EdgePadding);
                _waypointCache.Clear();
                _velocities.Clear();
            }
        }

        return _lastScreenSize != Vector2.Zero ? _lastScreenSize : args.Viewport.Size;
    }

    private Vector2 CalculateGameViewportSize(in OverlayDrawArgs args)
    {
        var worldViewport = _eye.GetWorldViewport();
        var topLeft = _eye.WorldToScreen(worldViewport.TopLeft);
        var bottomRight = _eye.WorldToScreen(worldViewport.BottomRight);

        var gameViewportSize = new Vector2(
            Math.Abs(bottomRight.X - topLeft.X),
            Math.Abs(bottomRight.Y - topLeft.Y));

        return gameViewportSize.X < 100 || gameViewportSize.Y < 100 ? args.Viewport.Size : gameViewportSize;
    }

    private bool ShouldDrawWaypoint(PingWaypointData waypointData, Vector2 screenSize)
    {
        if (!waypointData.EntityIsLoaded)
            return true;

        var screenPos = _eye.WorldToScreen(waypointData.WorldPosition);
        return !IsOnScreenWithMargin(screenPos, screenSize) || !IsValidScreenPosition(screenPos);
    }

    private static bool IsOnScreenWithMargin(Vector2 screenPos, Vector2 screenSize)
    {
        return screenPos.X >= -ScreenMargin &&
               screenPos.X <= screenSize.X + ScreenMargin &&
               screenPos.Y >= -ScreenMargin &&
               screenPos.Y <= screenSize.Y + ScreenMargin;
    }

    private void DrawWaypoint(DrawingHandleScreen handle, WaypointPositionData positionData)
    {
        var waypoint = positionData.Waypoint;
        if (waypoint.Texture == null)
            return;

        var time = (float) _timing.CurTime.TotalSeconds;
        var baseScale = waypoint.GroupCount > 1 ? BaseWaypointScale * 1.15f : BaseWaypointScale;

        var pulsePhase = time * PulseFrequency + waypoint.EntityUid.Id * 0.1f;
        var scale = baseScale * (1.0f + MathF.Sin(pulsePhase) * PulseAmplitude);

        var alpha = positionData.Distance <= FadeInDistance
            ? 1.0f
            : Math.Max(0.1f, 1.0f - (positionData.Distance - FadeInDistance) / _fadeRange);

        var textureSize = waypoint.Texture.Size * scale;
        var drawPos = positionData.Position - textureSize * 0.5f;

        var transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(drawPos);
        handle.SetTransform(transform);
        handle.DrawTexture(waypoint.Texture, Vector2.Zero, waypoint.Color.WithAlpha(alpha));
        handle.SetTransform(Matrix3x2.Identity);

        if (waypoint.GroupCount > 1)
            DrawGroupIndicator(handle, positionData.Position, waypoint.GroupCount, alpha);

        DrawWaypointInfo(handle, positionData.Position, waypoint, positionData.Distance, alpha);
    }

    private void DrawGroupIndicator(DrawingHandleScreen handle, Vector2 position, int count, float alpha)
    {
        var indicatorPos = position + new Vector2(18f, -18f);
        const float radius = 7f;

        handle.DrawCircle(indicatorPos, radius + 1f, Color.Black.WithAlpha(alpha * 0.6f), true);
        handle.DrawCircle(indicatorPos, radius, Color.FromHex("#2a4d3a").WithAlpha(alpha * 0.9f), true);
        handle.DrawCircle(indicatorPos, radius, Color.LimeGreen.WithAlpha(alpha * 0.8f));

        var countText = count.ToString();
        var textDimensions = handle.GetDimensions(_font, countText, 0.75f);
        var textPos = indicatorPos - textDimensions * 0.5f;
        handle.DrawString(_font, textPos, countText, 0.75f, Color.White.WithAlpha(alpha));
    }

    private void DrawWaypointInfo(DrawingHandleScreen handle, Vector2 position, PingWaypointData waypoint, float distance, float alpha)
    {
        var distanceText = distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000:F1}km";
        var creatorName = ExtractCreatorName(waypoint.Creator);
        var groupInfo = waypoint.GroupCount > 1 ? $" (x{waypoint.GroupCount})" : "";

        var distanceLine = distanceText;
        var creatorLine = $"{creatorName}{groupInfo}";

        var textColor = GetCreatorTextColor(waypoint.Creator).WithAlpha(alpha * 0.95f);
        var shadowColor = Color.Black.WithAlpha(alpha * 0.8f);
        const float scale = 0.85f;

        var distanceDimensions = handle.GetDimensions(_font, distanceLine, scale);
        var creatorDimensions = handle.GetDimensions(_font, creatorLine, scale);
        var maxWidth = Math.Max(distanceDimensions.X, creatorDimensions.X);

        var labelOffset = CalculateLabelOffset(position, maxWidth);
        var labelBasePos = position + labelOffset;

        var distancePos = labelBasePos + new Vector2(-distanceDimensions.X * 0.5f, 0f);
        DrawTextWithBackground(handle, distancePos, distanceLine, scale, textColor, shadowColor, alpha);

        var creatorPos = labelBasePos + new Vector2(-creatorDimensions.X * 0.5f, 12f);
        DrawTextWithBackground(handle, creatorPos, creatorLine, scale * 0.9f, textColor, shadowColor, alpha);
    }

    private Vector2 CalculateLabelOffset(Vector2 waypointPos, float textWidth)
    {
        var distToLeft = waypointPos.X - _screenBounds.Left;
        var distToRight = _screenBounds.Right - waypointPos.X;
        var distToTop = waypointPos.Y - _screenBounds.Top;
        var distToBottom = _screenBounds.Bottom - waypointPos.Y;

        var minDist = Math.Min(Math.Min(distToLeft, distToRight), Math.Min(distToTop, distToBottom));
        const float baseOffset = 18f;

        if (minDist == distToLeft)
            return new Vector2(baseOffset + textWidth * 0.3f, -8f);
        if (minDist == distToRight)
            return new Vector2(-baseOffset - textWidth * 0.3f, -8f);
        if (minDist == distToTop)
            return new Vector2(0f, baseOffset);
        if (minDist == distToBottom)
            return new Vector2(0f, -baseOffset - 16f);

        return new Vector2(0f, baseOffset);
    }

    private void DrawTextWithBackground(
        DrawingHandleScreen handle,
        Vector2 position,
        string text,
        float scale,
        Color textColor,
        Color shadowColor,
        float alpha)
    {
        var textDimensions = handle.GetDimensions(_font, text, scale);
        var padding = new Vector2(3f, 1f);

        var bgRect = new UIBox2(position - padding, position + textDimensions + padding);
        handle.DrawRect(bgRect, Color.Black.WithAlpha(alpha * 0.4f));
        handle.DrawString(_font, position + new Vector2(0.5f, 0.5f), text, scale, shadowColor);
        handle.DrawString(_font, position, text, scale, textColor);
    }

    private string ExtractCreatorName(EntityUid creator)
    {
        if (!Entity.EntityExists(creator))
            return "Unknown";

        var fullName = Entity.GetComponent<MetaDataComponent>(creator).EntityName ?? "Unknown";

        var openParen = fullName.LastIndexOf('(');
        if (openParen != -1)
        {
            var closeParen = fullName.LastIndexOf(')');
            if (closeParen > openParen)
                return fullName.Substring(openParen + 1, closeParen - openParen - 1);
        }

        return fullName.Length > 10 ? fullName[..7] + "..." : fullName;
    }

    private Vector2 CalculateTargetPosition(Vector2 worldPos, Vector2 playerPos)
    {
        var direction = GetDirectionToEdge(worldPos, playerPos);
        return CalculateEdgePosition(direction);
    }

    private static Vector2 GetDirectionToEdge(Vector2 worldPos, Vector2 playerPos)
    {
        var delta = worldPos - playerPos;
        if (delta.LengthSquared() < 0.0001f)
            return Vector2.UnitX;

        return Vector2.Normalize(new Vector2(delta.X, -delta.Y));
    }

    private Vector2 CalculateEdgePosition(Vector2 direction)
    {
        var bounds = _screenBounds;
        var center = _screenCenter;
        var rayTarget = center + direction * 1000f;
        var fallback = center + direction * 100f;
        var bestCandidate = fallback;
        var bestDistanceSquared = float.MaxValue;
        var hasCandidate = false;

        void TryCandidate(Vector2 candidate)
        {
            var distSq = Vector2.DistanceSquared(candidate, rayTarget);
            if (distSq < bestDistanceSquared)
            {
                bestDistanceSquared = distSq;
                bestCandidate = candidate;
                hasCandidate = true;
            }
        }

        if (direction.X > 0.001f)
        {
            var t = (bounds.Right - center.X) / direction.X;
            var y = center.Y + direction.Y * t;
            if (y >= bounds.Top && y <= bounds.Bottom)
                TryCandidate(new Vector2(bounds.Right, y));
        }

        if (direction.X < -0.001f)
        {
            var t = (bounds.Left - center.X) / direction.X;
            var y = center.Y + direction.Y * t;
            if (y >= bounds.Top && y <= bounds.Bottom)
                TryCandidate(new Vector2(bounds.Left, y));
        }

        if (direction.Y > 0.001f)
        {
            var t = (bounds.Bottom - center.Y) / direction.Y;
            var x = center.X + direction.X * t;
            if (x >= bounds.Left && x <= bounds.Right)
                TryCandidate(new Vector2(x, bounds.Bottom));
        }

        if (direction.Y < -0.001f)
        {
            var t = (bounds.Top - center.Y) / direction.Y;
            var x = center.X + direction.X * t;
            if (x >= bounds.Left && x <= bounds.Right)
                TryCandidate(new Vector2(x, bounds.Top));
        }

        return hasCandidate ? bestCandidate : fallback;
    }

    private static bool IsValidScreenPosition(Vector2 screenPos)
    {
        return !float.IsNaN(screenPos.X) &&
               !float.IsNaN(screenPos.Y) &&
               !float.IsInfinity(screenPos.X) &&
               !float.IsInfinity(screenPos.Y) &&
               Math.Abs(screenPos.X) < 50000 &&
               Math.Abs(screenPos.Y) < 50000;
    }

    private readonly record struct WaypointBounds(float Left, float Right, float Top, float Bottom)
    {
        public WaypointBounds(Vector2 screenSize, float padding) : this(
            padding,
            screenSize.X - padding,
            padding,
            screenSize.Y - padding)
        {
        }
    }

    private struct CachedWaypointData
    {
        public Vector2 Position;
        public TimeSpan LastUpdate;
    }

    private record struct WaypointPositionData
    {
        public PingWaypointData Waypoint;
        public Vector2 Position;
        public Vector2 TargetPosition;
        public float Distance;
        public int Priority;
        public float Radius;
    }
}
