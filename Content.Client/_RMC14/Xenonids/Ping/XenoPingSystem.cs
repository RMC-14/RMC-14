using Content.Shared._RMC14.Xenonids.Ping;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Ping;

public sealed class XenoPingSystem : SharedXenoPingSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private readonly Dictionary<EntityUid, PingWaypointData> _pingWaypoints = new();

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<XenoPingEntityComponent, ComponentInit>(OnPingEntityInit);
        SubscribeLocalEvent<XenoPingEntityComponent, ComponentShutdown>(OnPingEntityShutdown);

        _overlayManager.AddOverlay(new PingWaypointOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay<PingWaypointOverlay>();
        _pingWaypoints.Clear();
    }

    private void OnPingEntityInit(Entity<XenoPingEntityComponent> ent, ref ComponentInit args)
    {
        var creator = ent.Comp.Creator;
        var isQueen = EntityManager.HasComponent<XenoEvolutionGranterComponent>(creator);
        var isHiveLeader = EntityManager.HasComponent<HiveLeaderComponent>(creator);
        var shouldCreateWaypoint = isQueen || isHiveLeader;

        var color = GetColorFromPrototype(ent.Comp.PingType);
        if (TryComp<SpriteComponent>(ent.Owner, out var sprite))
        {
            sprite.Color = color;
            sprite.Visible = true;
        }

        if (!shouldCreateWaypoint)
        {
            return;
        }

        var xform = Transform(ent.Owner);
        var worldPos = _transform.GetWorldPosition(ent.Owner, _xformQuery);
        var originalCoords = xform.Coordinates;
        var mapId = xform.MapID;

        if (ent.Comp.AttachedTarget.HasValue && Exists(ent.Comp.AttachedTarget.Value))
        {
            worldPos = _transform.GetWorldPosition(ent.Comp.AttachedTarget.Value);
        }

        Robust.Client.Graphics.Texture? texture = null;
        if (sprite != null && sprite.BaseRSI != null && sprite[0].RsiState.IsValid)
        {
            if (sprite.BaseRSI.TryGetState(sprite[0].RsiState.Name, out var state))
            {
                texture = state.Frame0;
            }
        }

        var waypointData = new PingWaypointData(
            ent.Owner,
            ent.Comp.PingType,
            ent.Comp.Creator,
            worldPos,
            originalCoords,
            mapId,
            color,
            texture,
            ent.Comp.DeleteAt
        )
        {
            AttachedTarget = ent.Comp.AttachedTarget,
            IsTargetValid = ent.Comp.AttachedTarget.HasValue && Exists(ent.Comp.AttachedTarget.Value)
        };

        _pingWaypoints[ent.Owner] = waypointData;
    }

    private Color GetColorFromPrototype(string pingType)
    {
        var entityId = GetPingEntityId(pingType);

        if (!_prototypeManager.TryIndex<EntityPrototype>(entityId, out var prototype))
        {
            return Color.White;
        }

        if (prototype.Components.TryGetValue("Sprite", out var spriteData))
        {
            try
            {
                var spriteComp = (SpriteComponent)spriteData.Component;
                return spriteComp.Color;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while retrieving the color: {ex.Message}");
            }
        }

        return Color.White;
    }

    private void OnPingEntityShutdown(Entity<XenoPingEntityComponent> ent, ref ComponentShutdown args)
    {
        _pingWaypoints.Remove(ent.Owner);
    }

    public override void FrameUpdate(float frameTime)
    {
        var player = _playerManager.LocalEntity;
        if (player == null)
            return;

        var playerPos = _transform.GetWorldPosition(player.Value);

        var query = EntityQueryEnumerator<XenoPingEntityComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var ping, out var sprite, out var xform))
        {
            var pingPos = _transform.GetWorldPosition(uid, _xformQuery);

            if (_pingWaypoints.TryGetValue(uid, out var waypointData))
            {
                if (ping.AttachedTarget.HasValue && Exists(ping.AttachedTarget.Value))
                {
                    var targetPos = _transform.GetWorldPosition(ping.AttachedTarget.Value);
                    waypointData.WorldPosition = targetPos;
                    waypointData.IsTargetValid = true;

                    var targetXform = Transform(ping.AttachedTarget.Value);
                    ping.LastKnownCoordinates = targetXform.Coordinates;

                    _transform.SetWorldPosition(uid, targetPos);
                }
                else if (ping.AttachedTarget.HasValue && !Exists(ping.AttachedTarget.Value))
                {
                    waypointData.IsTargetValid = false;
                    if (ping.LastKnownCoordinates.HasValue)
                    {
                        var lastWorldPos = _transform.ToMapCoordinates(ping.LastKnownCoordinates.Value);
                        waypointData.WorldPosition = lastWorldPos.Position;

                        _transform.SetCoordinates(uid, ping.LastKnownCoordinates.Value);
                    }
                }
                else
                {
                    if (pingPos != Vector2.Zero || waypointData.WorldPosition == Vector2.Zero)
                    {
                        waypointData.WorldPosition = pingPos;
                    }
                }

                waypointData.EntityIsLoaded = true;

                if (sprite.Color != Color.White && sprite.Color != waypointData.Color)
                {
                    waypointData.Color = sprite.Color;
                }

                if (waypointData.Texture == null && sprite.BaseRSI != null && sprite[0].RsiState.IsValid)
                {
                    if (sprite.BaseRSI.TryGetState(sprite[0].RsiState.Name, out var state))
                    {
                        waypointData.Texture = state.Frame0;
                    }
                }
            }
        }

        var loadedEntities = new HashSet<EntityUid>();
        var queryCheck = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (queryCheck.MoveNext(out var uid, out _))
        {
            loadedEntities.Add(uid);
        }

        foreach (var (uid, data) in _pingWaypoints)
        {
            data.EntityIsLoaded = loadedEntities.Contains(uid);
        }
    }

    public override void Update(float frameTime)
    {
        var currentTime = _timing.CurTime;
        var toRemove = new List<EntityUid>();

        foreach (var (uid, data) in _pingWaypoints)
        {
            if (currentTime >= data.DeleteAt)
            {
                toRemove.Add(uid);
            }
        }

        foreach (var uid in toRemove)
        {
            _pingWaypoints.Remove(uid);
        }
    }

    public IReadOnlyDictionary<EntityUid, PingWaypointData> GetPingWaypoints() => _pingWaypoints;

    public Dictionary<string, (string Name, Color Color, string Description)> GetAvailablePingTypesWithColors()
    {
        var basePings = SharedXenoPingSystem.GetAvailablePingTypes();
        var result = new Dictionary<string, (string Name, Color Color, string Description)>();

        foreach (var (pingType, (name, description)) in basePings)
        {
            var color = GetColorFromPrototype(pingType);
            result[pingType] = (name, color, description);
        }

        return result;
    }

    public Dictionary<string, (string Name, Color Color, string Description)> GetAvailableConstructionPingTypesWithColors()
    {
        var basePings = SharedXenoPingSystem.GetAvailableConstructionPingTypes();
        var result = new Dictionary<string, (string Name, Color Color, string Description)>();

        foreach (var (pingType, (name, description)) in basePings)
        {
            var color = GetColorFromPrototype(pingType);
            result[pingType] = (name, color, description);
        }

        return result;
    }
}
