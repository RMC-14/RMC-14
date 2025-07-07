using System.Numerics;
using Content.Client.Resources;
using Content.Shared._RMC14.Xenonids.Ping;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client._RMC14.Xenonids.Ping;

//this code is cursed
public sealed class PingWaypointOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly ShaderInstance _shader;
    private readonly Font _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const float EdgePadding = 120f;
    private const float BaseWaypointScale = 1.5f;
    private const float PulseAmplitude = 0.08f;
    private const float PulseFrequency = 1.8f;
    private const float FadeInDistance = 300f;
    private const float MaxVisibleDistance = 2000f;
    private const float MaxVisibleDistanceSquared = MaxVisibleDistance * MaxVisibleDistance;
    private const float ScreenMargin = 50f;

    // layout
    private const float WaypointRadius = 28f;
    private const float MinSeparationDistance = 5f;
    private const float MaxOverlapResolutionDistance = 150f;
    private const int MaxOverlapIterations = 4;
    private const float CornerAvoidanceRadius = 40f;
    private const float EdgePreferenceWeight = 0.4f;
    private const float RepulsionForce = 10f;

    // grouping
    private const float GroupingDistance = 180f;
    private const float GroupingDistanceSquared = GroupingDistance * GroupingDistance;
    private const float MinGroupingViewDistance = 25f;
    private const float MinGroupingViewDistanceSquared = MinGroupingViewDistance * MinGroupingViewDistance;

    // cahe
    private Vector2 _lastScreenSize = Vector2.Zero;
    private TimeSpan _lastScreenSizeCheck = TimeSpan.Zero;
    private const double ScreenSizeCheckInterval = 0.3;
    private const double PositionCacheTimeout = 0.8;

    // pre-calculated screen values (updated when screen size changes)
    private Vector2 _screenCenter;
    private WaypointBounds _screenBounds;
    private float _fadeRange;

    private readonly Dictionary<EntityUid, CachedWaypointData> _waypointCache = new();
    private readonly List<WaypointPositionData> _workingPositions = new();
    private readonly HashSet<EntityUid> _expiredEntities = new();

    private readonly Dictionary<EntityUid, Vector2> _velocities = new();
    private const float Damping = 0.85f;
    private const float MaxVelocity = 5f;

    public PingWaypointOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();
        _shader = _prototype.Index<ShaderPrototype>("unshaded").Instance();
        _font = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 9);

        ZIndex = 100;

        _fadeRange = MaxVisibleDistance - FadeInDistance;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _players.LocalEntity;
        if (!player.HasValue) return;

        var handle = args.ScreenHandle;
        var screenSize = CheckAndUpdateScreenSize(args);

        handle.UseShader(_shader);

        var playerPos = _transform.GetWorldPosition(player.Value);
        var waypoints = _entity.System<XenoPingSystem>().GetPingWaypoints();

        CleanExpiredEntities(waypoints);

        var groupedWaypoints = GroupWaypoints(waypoints, playerPos);

        CalculateAndResolvePositions(groupedWaypoints, playerPos, screenSize);

        foreach (var positionData in _workingPositions)
        {
            DrawWaypoint(handle, positionData);
        }

        handle.UseShader(null);
        _workingPositions.Clear();
    }

    private void CalculateAndResolvePositions(List<PingWaypointData> waypoints, Vector2 playerPos, Vector2 screenSize)
    {
        var currentTime = _timing.CurTime;

        foreach (var waypoint in waypoints)
        {
            if (!waypoint.IsValid || waypoint.Texture == null) continue;

            var distanceSquared = Vector2.DistanceSquared(waypoint.WorldPosition, playerPos);
            if (distanceSquared > MaxVisibleDistanceSquared) continue;

            if (!ShouldDrawWaypoint(waypoint, screenSize)) continue;

            var distance = MathF.Sqrt(distanceSquared);
            var targetPosition = CalculateTargetPosition(waypoint.WorldPosition, playerPos);

            // use cache if possible
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
                    LastUpdate = currentTime,
                    Priority = GetWaypointPriority(waypoint)
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

        // force based collision
        ResolveCollisionsWithForces(currentTime);
    }

    private void ResolveCollisionsWithForces(TimeSpan currentTime)
    {
        var deltaTime = 1f / 60f; //assuming 60fps

        _workingPositions.Sort((a, b) =>
        {
            var priorityComp = a.Priority.CompareTo(b.Priority);
            if (priorityComp != 0) return priorityComp;
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

                // attraction to target positions
                var targetForce = (current.TargetPosition - current.Position) * 0.3f;
                force += targetForce;

                // repulsion
                for (var j = 0; j < _workingPositions.Count; j++)
                {
                    if (i == j) continue;

                    var other = _workingPositions[j];
                    var separation = current.Position - other.Position;
                    var distance = separation.Length();
                    var minDistance = current.Radius + other.Radius + MinSeparationDistance;

                    if (distance < minDistance && distance > 0.001f)
                    {
                        var overlap = minDistance - distance;
                        var repulsion = separation / distance * overlap * RepulsionForce;

                        // higher priority waypoints push away lower priority ones more strongly
                        if (current.Priority <= other.Priority)
                        {
                            repulsion *= 1.5f;
                        }

                        force += repulsion;
                    }
                }

                // boundary forces (keep them on screen)
                force += CalculateBoundaryForces(current.Position, current.Radius);

                _velocities[uid] = (_velocities[uid] + force * deltaTime) * Damping;

                if (_velocities[uid].LengthSquared() > MaxVelocity * MaxVelocity)
                {
                    _velocities[uid] = Vector2.Normalize(_velocities[uid]) * MaxVelocity;
                }

                // increase
                var newPosition = current.Position + _velocities[uid] * deltaTime * 3f;
                newPosition = ClampToScreenBounds(newPosition, current.Radius);

                if (Vector2.Distance(current.Position, newPosition) > 0.2f)
                {
                    hasSignificantMovement = true;
                }

                _workingPositions[i] = current with { Position = newPosition };

                // Update cache
                _waypointCache[uid] = new CachedWaypointData
                {
                    Position = newPosition,
                    LastUpdate = currentTime,
                    Priority = current.Priority
                };
            }

            // if positions have stabilized, stop iterating
            if (!hasSignificantMovement)
                break;
        }
    }

    private Vector2 CalculateBoundaryForces(Vector2 position, float radius)
    {
        var force = Vector2.Zero;
        var pushForce = 25f;

        // left
        if (position.X - radius < _screenBounds.Left)
            force.X += pushForce;

        // right
        if (position.X + radius > _screenBounds.Right)
            force.X -= pushForce;

        // top
        if (position.Y - radius < _screenBounds.Top)
            force.Y += pushForce;

        // bottom
        if (position.Y + radius > _screenBounds.Bottom)
            force.Y -= pushForce;

        return force;
    }

    private Vector2 ClampToScreenBounds(Vector2 position, float radius)
    {
        return new Vector2(
            Math.Clamp(position.X, _screenBounds.Left + radius, _screenBounds.Right - radius),
            Math.Clamp(position.Y, _screenBounds.Top + radius, _screenBounds.Bottom - radius)
        );
    }

    private void CleanExpiredEntities(IReadOnlyDictionary<EntityUid, PingWaypointData> waypoints)
    {
        _expiredEntities.Clear();

        foreach (var uid in _waypointCache.Keys)
        {
            if (!waypoints.ContainsKey(uid))
            {
                _expiredEntities.Add(uid);
            }
        }

        foreach (var uid in _expiredEntities)
        {
            _waypointCache.Remove(uid);
            _velocities.Remove(uid);
        }
    }

    private List<PingWaypointData> GroupWaypoints(IReadOnlyDictionary<EntityUid, PingWaypointData> waypoints, Vector2 playerPos)
    {
        var result = new List<PingWaypointData>();
        var processed = new HashSet<EntityUid>();

        foreach (var (uid, waypoint) in waypoints)
        {
            if (processed.Contains(uid)) continue;

            var playerDistanceSquared = Vector2.DistanceSquared(waypoint.WorldPosition, playerPos);
            if (playerDistanceSquared < MinGroupingViewDistanceSquared)
            {
                result.Add(waypoint);
                processed.Add(uid);
                continue;
            }

            // all nearby waypoints from the same creator with the same ping type
            var group = new List<PingWaypointData> { waypoint };
            processed.Add(uid);

            foreach (var (otherUid, otherWaypoint) in waypoints)
            {
                if (processed.Contains(otherUid)) continue;
                if (otherWaypoint.Creator != waypoint.Creator) continue;
                if (otherWaypoint.PingType != waypoint.PingType) continue;

                var distanceSquared = Vector2.DistanceSquared(waypoint.WorldPosition, otherWaypoint.WorldPosition);
                if (distanceSquared <= GroupingDistanceSquared)
                {
                    group.Add(otherWaypoint);
                    processed.Add(otherUid);
                }
            }

            result.Add(group.Count > 1 ? CreateGroupedWaypoint(group) : waypoint);
        }

        return result;
    }

    private PingWaypointData CreateGroupedWaypoint(List<PingWaypointData> group)
    {
        // weighted center based on recency
        var totalWeight = 0f;
        var weightedCenter = Vector2.Zero;
        var currentTime = _timing.CurTime;

        foreach (var waypoint in group)
        {
            var age = (float)(currentTime - waypoint.DeleteAt + waypoint.DeleteAt).TotalSeconds;
            var weight = Math.Max(0.1f, 1f - age / 30f); // newer waypoints have more weight
            weightedCenter += waypoint.WorldPosition * weight;
            totalWeight += weight;
        }

        weightedCenter /= totalWeight;

        var baseWaypoint = group.OrderByDescending(w => currentTime - w.DeleteAt + w.DeleteAt).First();

        return new PingWaypointData(
            baseWaypoint.EntityUid,
            baseWaypoint.PingType,
            baseWaypoint.Creator,
            weightedCenter,
            baseWaypoint.OriginalCoordinates,
            baseWaypoint.MapId,
            baseWaypoint.Color,
            baseWaypoint.Texture,
            group.Max(w => w.DeleteAt)
        )
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

                // Clear caches on screen resize
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
            Math.Abs(bottomRight.Y - topLeft.Y)
        );

        return gameViewportSize.X < 100 || gameViewportSize.Y < 100 ? args.Viewport.Size : gameViewportSize;
    }

    private bool ShouldDrawWaypoint(PingWaypointData waypointData, Vector2 screenSize)
    {
        if (!waypointData.EntityIsLoaded) return true;

        var screenPos = _eye.WorldToScreen(waypointData.WorldPosition);
        return !IsOnScreenWithMargin(screenPos, screenSize) || !IsValidScreenPosition(screenPos);
    }

    private bool IsOnScreenWithMargin(Vector2 screenPos, Vector2 screenSize)
    {
        return screenPos.X >= -ScreenMargin &&
               screenPos.X <= screenSize.X + ScreenMargin &&
               screenPos.Y >= -ScreenMargin &&
               screenPos.Y <= screenSize.Y + ScreenMargin;
    }

    private void DrawWaypoint(DrawingHandleScreen handle, WaypointPositionData positionData)
    {
        var waypoint = positionData.Waypoint;
        if (waypoint.Texture == null) return;

        var time = (float)_timing.CurTime.TotalSeconds;
        var baseScale = waypoint.GroupCount > 1 ? BaseWaypointScale * 1.15f : BaseWaypointScale;

        // pulsing animation
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
        {
            DrawGroupIndicator(handle, positionData.Position, waypoint.GroupCount, alpha);
        }

        DrawWaypointInfo(handle, positionData.Position, waypoint, positionData.Distance, alpha);
    }

    private void DrawGroupIndicator(DrawingHandleScreen handle, Vector2 position, int count, float alpha)
    {
        // Draw a small circle with the count number
        var indicatorPos = position + new Vector2(22f, -22f);
        var radius = 9f;

        // Draw background circle
        handle.DrawCircle(indicatorPos, radius + 1f, Color.Black.WithAlpha(alpha * 0.6f), true);
        handle.DrawCircle(indicatorPos, radius, Color.FromHex("#2a4d3a").WithAlpha(alpha * 0.9f), true);
        handle.DrawCircle(indicatorPos, radius, Color.LimeGreen.WithAlpha(alpha * 0.8f));

        // Draw count text
        var countText = count.ToString();
        var textDimensions = handle.GetDimensions(_font, countText, 0.75f);
        var textPos = indicatorPos - textDimensions * 0.5f;
        handle.DrawString(_font, textPos, countText, 0.75f, Color.White.WithAlpha(alpha));
    }

    private void DrawWaypointInfo(DrawingHandleScreen handle, Vector2 position, PingWaypointData waypoint, float distance, float alpha)
    {
        var distanceText = distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000:F1}km";
        var creatorName = ExtractCreatorName(waypoint.Creator);
        var groupInfo = waypoint.GroupCount > 1 ? $" (×{waypoint.GroupCount})" : "";

        var distanceLine = distanceText;
        var creatorLine = $"{creatorName}{groupInfo}";

        var textColor = GetCreatorTextColor(waypoint.Creator).WithAlpha(alpha * 0.95f);
        var shadowColor = Color.Black.WithAlpha(alpha * 0.8f);
        var scale = 0.85f;

        // text dimensions for positioning
        var distanceDimensions = handle.GetDimensions(_font, distanceLine, scale);
        var creatorDimensions = handle.GetDimensions(_font, creatorLine, scale);
        var maxWidth = Math.Max(distanceDimensions.X, creatorDimensions.X);

        // label position based on waypoint location
        var labelOffset = CalculateLabelOffset(position, maxWidth);
        var labelBasePos = position + labelOffset;

        // distance text
        var distancePos = labelBasePos + new Vector2(-distanceDimensions.X * 0.5f, 0f);
        DrawTextWithBackground(handle, distancePos, distanceLine, scale, textColor, shadowColor, alpha);

        // creator text below distance
        var creatorPos = labelBasePos + new Vector2(-creatorDimensions.X * 0.5f, 12f);
        DrawTextWithBackground(handle, creatorPos, creatorLine, scale * 0.9f, textColor, shadowColor, alpha);
    }

    private Vector2 CalculateLabelOffset(Vector2 waypointPos, float textWidth)
    {
        // which edge the waypoint is closest to
        var distToLeft = waypointPos.X - _screenBounds.Left;
        var distToRight = _screenBounds.Right - waypointPos.X;
        var distToTop = waypointPos.Y - _screenBounds.Top;
        var distToBottom = _screenBounds.Bottom - waypointPos.Y;

        var minDist = Math.Min(Math.Min(distToLeft, distToRight), Math.Min(distToTop, distToBottom));

        // base offset distance - closer to waypoint
        var baseOffset = 18f;

        // label position based on closest edge to avoid going off-screen
        if (minDist == distToLeft)
            return new Vector2(baseOffset + textWidth * 0.3f, -8f);
        else if (minDist == distToRight)
            return new Vector2(-baseOffset - textWidth * 0.3f, -8f);
        else if (minDist == distToTop)
            return new Vector2(0f, baseOffset);
        else if (minDist == distToBottom)
            return new Vector2(0f, -baseOffset - 16f);
        else
            return new Vector2(0f, baseOffset);
    }

    private void DrawTextWithBackground(DrawingHandleScreen handle, Vector2 position, string text, float scale, Color textColor, Color shadowColor, float alpha)
    {
        var textDimensions = handle.GetDimensions(_font, text, scale);
        var padding = new Vector2(3f, 1f);

        var bgRect = new UIBox2(
            position - padding,
            position + textDimensions + padding
        );

        handle.DrawRect(bgRect, Color.Black.WithAlpha(alpha * 0.4f));
        handle.DrawString(_font, position + new Vector2(0.5f, 0.5f), text, scale, shadowColor);
        handle.DrawString(_font, position, text, scale, textColor);
    }

    private Color GetCreatorTextColor(EntityUid creator)
    {
        if (!_entity.EntityExists(creator))
            return Color.White;

        if (_entity.HasComponent<XenoEvolutionGranterComponent>(creator))
            return Color.Gold;

        if (_entity.HasComponent<HiveLeaderComponent>(creator))
            return Color.Orange;

        return Color.LightGray;
    }

    private string ExtractCreatorName(EntityUid creator)
    {
        if (!_entity.EntityExists(creator))
            return "Unknown";

        var fullName = _entity.GetComponent<MetaDataComponent>(creator).EntityName ?? "Unknown";

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

    private Vector2 GetDirectionToEdge(Vector2 worldPos, Vector2 playerPos)
    {
        var delta = worldPos - playerPos;
        if (delta.LengthSquared() < 0.0001f) return Vector2.UnitX;

        return Vector2.Normalize(new Vector2(delta.X, -delta.Y));
    }

    private Vector2 CalculateEdgePosition(Vector2 direction)
    {
        var bounds = _screenBounds;
        var center = _screenCenter;

        // calculate intersection with screen boundaries
        var candidates = new List<Vector2>();

        // right edge
        if (direction.X > 0.001f)
        {
            var t = (bounds.Right - center.X) / direction.X;
            var y = center.Y + direction.Y * t;
            if (y >= bounds.Top && y <= bounds.Bottom)
                candidates.Add(new Vector2(bounds.Right, y));
        }

        // left edge
        if (direction.X < -0.001f)
        {
            var t = (bounds.Left - center.X) / direction.X;
            var y = center.Y + direction.Y * t;
            if (y >= bounds.Top && y <= bounds.Bottom)
                candidates.Add(new Vector2(bounds.Left, y));
        }

        // bottom edge
        if (direction.Y > 0.001f)
        {
            var t = (bounds.Bottom - center.Y) / direction.Y;
            var x = center.X + direction.X * t;
            if (x >= bounds.Left && x <= bounds.Right)
                candidates.Add(new Vector2(x, bounds.Bottom));
        }

        // top edge
        if (direction.Y < -0.001f)
        {
            var t = (bounds.Top - center.Y) / direction.Y;
            var x = center.X + direction.X * t;
            if (x >= bounds.Left && x <= bounds.Right)
                candidates.Add(new Vector2(x, bounds.Top));
        }

        // return closest valid intersection
        return candidates.Count > 0
            ? candidates.OrderBy(p => Vector2.Distance(p, center + direction * 1000f)).First()
            : center + direction * 100f; // fallback
    }

    private int GetWaypointPriority(PingWaypointData waypoint)
    {
        if (_entity.HasComponent<XenoEvolutionGranterComponent>(waypoint.Creator))
            return 0; // queens have highest priority
        if (_entity.HasComponent<HiveLeaderComponent>(waypoint.Creator))
            return 1; // hive leaders second
        return 2; // xenos lowest
    }

    private float GetWaypointRadius(PingWaypointData waypoint)
    {
        var baseRadius = WaypointRadius;

        if (waypoint.GroupCount > 1)
            baseRadius *= 1.2f;

        if (_entity.HasComponent<XenoEvolutionGranterComponent>(waypoint.Creator))
            baseRadius *= 1.1f;

        return baseRadius;
    }

    private static bool IsValidScreenPosition(Vector2 screenPos) =>
        !float.IsNaN(screenPos.X) && !float.IsNaN(screenPos.Y) &&
        !float.IsInfinity(screenPos.X) && !float.IsInfinity(screenPos.Y) &&
        Math.Abs(screenPos.X) < 50000 && Math.Abs(screenPos.Y) < 50000;

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
        public int Priority;
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
