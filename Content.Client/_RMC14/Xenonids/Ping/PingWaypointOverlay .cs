using System.Numerics;
using Content.Client.Resources;
using Content.Shared._RMC14.Xenonids.Ping;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
    private const float PulseAmplitude = 0.1f;
    private const float PulseFrequency = 2.0f;
    private const float FadeInDistance = 300f;
    private const float MaxVisibleDistance = 2000f;
    private const float MaxVisibleDistanceSquared = MaxVisibleDistance * MaxVisibleDistance;
    private const float ScreenMargin = 50f;

    // grouping consts
    private const float GroupingDistance = 150f;
    private const float GroupingDistanceSquared = GroupingDistance * GroupingDistance;
    private const float MinGroupingViewDistance = 50f;
    private const float MinGroupingViewDistanceSquared = MinGroupingViewDistance * MinGroupingViewDistance;

    // screen size
    private Vector2 _lastScreenSize = Vector2.Zero;
    private TimeSpan _lastScreenSizeCheck = TimeSpan.Zero;
    private const double ScreenSizeCheckInterval = 0.5;

    // pre-calculated screen values (updated when screen size changes)
    private Vector2 _screenCenter;
    private WaypointBounds _screenBounds;
    private float _fadeRange;

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

        var groupedWaypoints = GroupWaypoints(waypoints, playerPos);

        foreach (var waypoint in groupedWaypoints)
        {
            if (!waypoint.IsValid || waypoint.Texture == null) continue;

            // fast distance check using squared distance (avoids sqrt)
            var deltaX = waypoint.WorldPosition.X - playerPos.X;
            var deltaY = waypoint.WorldPosition.Y - playerPos.Y;
            var distanceSquared = deltaX * deltaX + deltaY * deltaY;

            if (distanceSquared > MaxVisibleDistanceSquared) continue;

            if (ShouldDrawWaypoint(waypoint, screenSize))
            {
                var distance = MathF.Sqrt(distanceSquared); // only calculate sqrt when needed
                var waypointPos = CalculateWaypointPositionFast(waypoint.WorldPosition, playerPos);
                DrawWaypoint(handle, waypointPos, waypoint, distance);
            }
        }

        handle.UseShader(null);
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

            if (group.Count > 1)
            {
                var groupedWaypoint = CreateGroupedWaypoint(group);
                result.Add(groupedWaypoint);
            }
            else
            {
                result.Add(waypoint);
            }
        }

        return result;
    }

    private PingWaypointData CreateGroupedWaypoint(List<PingWaypointData> group)
    {
        // centre position
        var centerPos = Vector2.Zero;
        foreach (var waypoint in group)
        {
            centerPos += waypoint.WorldPosition;
        }
        centerPos /= group.Count;

        // 1st waypoint as base
        var baseWaypoint = group[0];
        var groupedWaypoint = new PingWaypointData(
            baseWaypoint.EntityUid, // keep original UID
            baseWaypoint.PingType,
            baseWaypoint.Creator,
            centerPos,
            baseWaypoint.OriginalCoordinates,
            baseWaypoint.MapId,
            baseWaypoint.Color,
            baseWaypoint.Texture,
            baseWaypoint.DeleteAt // latest delete time
        )
        {
            EntityIsLoaded = baseWaypoint.EntityIsLoaded,
            GroupCount = group.Count
        };

        foreach (var waypoint in group)
        {
            if (waypoint.DeleteAt > groupedWaypoint.DeleteAt)
            {
                groupedWaypoint.DeleteAt = waypoint.DeleteAt;
            }
        }

        return groupedWaypoint;
    }

    private Vector2 CheckAndUpdateScreenSize(in OverlayDrawArgs args)
    {
        var currentTime = _timing.CurTime;

        if (currentTime - _lastScreenSizeCheck >= TimeSpan.FromSeconds(ScreenSizeCheckInterval))
        {
            _lastScreenSizeCheck = currentTime;

            var gameViewportSize = CalculateGameViewportSize(args);
            var sizeDiff = gameViewportSize - _lastScreenSize;

            if (MathF.Abs(sizeDiff.X) > 10 || MathF.Abs(sizeDiff.Y) > 10)
            {
                _lastScreenSize = gameViewportSize;

                // update screen center & bounds
                _screenCenter = gameViewportSize * 0.5f;
                _screenBounds = new WaypointBounds(gameViewportSize, EdgePadding);

                // force waypoint recalculation
                var pingSystem = _entity.System<XenoPingSystem>();
                var cachedWaypoints = pingSystem.GetPingWaypoints();
                foreach (var (_, waypointData) in cachedWaypoints)
                {
                    waypointData.EntityIsLoaded = false;
                }
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
            MathF.Abs(bottomRight.X - topLeft.X),
            MathF.Abs(bottomRight.Y - topLeft.Y)
        );

        var fullScreenSize = args.Viewport.Size;
        return gameViewportSize.X < 100 || gameViewportSize.Y < 100 ? fullScreenSize : gameViewportSize;
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

    private void DrawWaypoint(DrawingHandleScreen handle, Vector2 position, PingWaypointData waypointData, float distance)
    {
        if (waypointData.Texture == null)
            return;

        // animation scale - larger for grouped waypoints
        var time = (float)_timing.CurTime.TotalSeconds;
        var baseScale = waypointData.GroupCount > 1 ? BaseWaypointScale * 1.2f : BaseWaypointScale;
        var scale = baseScale * (1.0f + MathF.Sin(time * PulseFrequency) * PulseAmplitude);

        // calculate alpha based on distance
        var alpha = distance <= FadeInDistance ? 1.0f : 1.0f - (distance - FadeInDistance) / _fadeRange;

        var textureSize = waypointData.Texture.Size * scale;
        var drawPos = position - textureSize * 0.5f;

        var transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(drawPos);
        handle.SetTransform(transform);
        handle.DrawTexture(waypointData.Texture, Vector2.Zero, waypointData.Color.WithAlpha(alpha));
        handle.SetTransform(Matrix3x2.Identity);

        if (waypointData.GroupCount > 1)
        {
            DrawGroupIndicator(handle, position, waypointData.GroupCount, alpha);
        }

        DrawWaypointInfo(handle, position, waypointData, distance);
    }

    private void DrawGroupIndicator(DrawingHandleScreen handle, Vector2 position, int count, float alpha)
    {
        // Draw a small circle with the count number
        var indicatorPos = position + new Vector2(20f, -20f);
        var indicatorSize = new Vector2(16f, 16f);

        // Draw background circle
        handle.DrawCircle(indicatorPos, indicatorSize.X * 0.5f, Color.Black.WithAlpha(alpha * 0.8f), true);
        handle.DrawCircle(indicatorPos, indicatorSize.X * 0.5f, Color.White.WithAlpha(alpha), false);

        // Draw count text
        var countText = count.ToString();
        var textDimensions = handle.GetDimensions(_font, countText, 0.8f);
        var textPos = indicatorPos - textDimensions * 0.5f;
        handle.DrawString(_font, textPos, countText, 0.8f, Color.White.WithAlpha(alpha));
    }

    private void DrawWaypointInfo(DrawingHandleScreen handle, Vector2 position, PingWaypointData waypointData, float distance)
    {
        var distanceText = distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000:F1}km";
        var creatorName = ExtractCreatorName(waypointData.Creator);

        // Add group info if applicable
        var groupInfo = waypointData.GroupCount > 1 ? $" (x{waypointData.GroupCount})" : "";
        var infoText = $"{distanceText} {creatorName}{groupInfo}";

        var textDimensions = handle.GetDimensions(_font, infoText, 1f);
        var textPos = position + new Vector2(-textDimensions.X * 0.5f, 18f);

        handle.DrawString(_font, textPos, infoText, Color.White);
    }

    private string ExtractCreatorName(EntityUid creator)
    {
        if (!_entity.EntityExists(creator))
            return "Unknown";

        var fullName = _entity.GetComponent<MetaDataComponent>(creator).EntityName ?? "Unknown";

        // extract name
        var openParen = fullName.LastIndexOf('(');
        if (openParen != -1)
        {
            var closeParen = fullName.LastIndexOf(')');
            if (closeParen > openParen)
                return fullName.Substring(openParen + 1, closeParen - openParen - 1);
        }

        // never be used but just incase
        return fullName.Length > 8 ? fullName[..5] + "..." : fullName;
    }

    private Vector2 CalculateWaypointPositionFast(Vector2 worldPos, Vector2 playerPos)
    {
        var direction = GetDirectionFast(worldPos, playerPos);
        return CalculateEdgePositionFast(direction);
    }

    private Vector2 GetDirectionFast(Vector2 worldPos, Vector2 playerPos)
    {
        var deltaX = worldPos.X - playerPos.X;
        var deltaY = worldPos.Y - playerPos.Y;

        var lengthSquared = deltaX * deltaX + deltaY * deltaY;
        if (lengthSquared < 0.0001f) return Vector2.UnitX;

        var invLength = 1.0f / MathF.Sqrt(lengthSquared);
        return new Vector2(deltaX * invLength, -deltaY * invLength);
    }

    private Vector2 CalculateEdgePositionFast(Vector2 direction)
    {
        var bounds = _screenBounds;
        var screenCenter = _screenCenter;

        float? tVertical = null;
        float? tHorizontal = null;

        // calculate intersection with vertical edges
        if (MathF.Abs(direction.X) > 0.001f)
        {
            var targetX = direction.X > 0 ? bounds.Right : bounds.Left;
            tVertical = (targetX - screenCenter.X) / direction.X;
        }

        // calculate intersection with horizontal edges
        if (MathF.Abs(direction.Y) > 0.001f)
        {
            var targetY = direction.Y > 0 ? bounds.Bottom : bounds.Top;
            tHorizontal = (targetY - screenCenter.Y) / direction.Y;
        }

        // find closest intersection
        var t = float.MaxValue;
        if (tVertical.HasValue && tVertical.Value > 0)
            t = MathF.Min(t, tVertical.Value);
        if (tHorizontal.HasValue && tHorizontal.Value > 0)
            t = MathF.Min(t, tHorizontal.Value);

        Vector2 edgePosition;
        if (t != float.MaxValue)
        {
            edgePosition = screenCenter + direction * t;
        }
        else
        {
            // fallback to direct edge calculation
            var x = direction.X > 0 ? bounds.Right : (direction.X < 0 ? bounds.Left : screenCenter.X);
            var y = direction.Y > 0 ? bounds.Bottom : (direction.Y < 0 ? bounds.Top : screenCenter.Y);
            edgePosition = new Vector2(x, y);
        }

        // clamping to the screen boundaries
        return new Vector2(
            MathF.Max(bounds.Left, MathF.Min(bounds.Right, edgePosition.X)),
            MathF.Max(bounds.Top, MathF.Min(bounds.Bottom, edgePosition.Y))
        );
    }

    private static bool IsValidScreenPosition(Vector2 screenPos) =>
        !float.IsNaN(screenPos.X) && !float.IsNaN(screenPos.Y) &&
        !float.IsInfinity(screenPos.X) && !float.IsInfinity(screenPos.Y) &&
        MathF.Abs(screenPos.X) < 100000 && MathF.Abs(screenPos.Y) < 100000;

    private readonly struct WaypointBounds
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Top;
        public readonly float Bottom;

        public WaypointBounds(Vector2 screenSize, float padding)
        {
            Left = padding;
            Right = screenSize.X - padding;
            Top = padding;
            Bottom = screenSize.Y - padding;
        }
    }
}



