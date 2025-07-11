using Content.Shared._RMC14.Xenonids.Ping;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
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
        if (TryComp<SpriteComponent>(ent.Owner, out var sprite))
            sprite.Visible = false;

        ProcessPingVisibility(ent.Owner);
    }

    private void ProcessPingVisibility(EntityUid pingUid)
    {
        if (!Exists(pingUid) || !TryComp<XenoPingEntityComponent>(pingUid, out var pingComp))
            return;

        if (pingComp.Creator == EntityUid.Invalid)
        {
            Timer.Spawn(TimeSpan.FromMilliseconds(50), () => ProcessPingVisibility(pingUid));
            return;
        }

        if (!ShouldShowPing(pingComp.Creator))
        {
            return;
        }

        if (TryComp<SpriteComponent>(pingUid, out var spriteComp))
            spriteComp.Visible = true;

        var creator = pingComp.Creator;
        var isQueen = EntityManager.HasComponent<XenoEvolutionGranterComponent>(creator);
        var isHiveLeader = EntityManager.HasComponent<HiveLeaderComponent>(creator);

        if (isQueen || isHiveLeader)
            CreateWaypoint(pingUid, pingComp);
    }

    private void CreateWaypoint(EntityUid pingUid, XenoPingEntityComponent pingComp)
    {
        var color = GetColorFromEntity(pingUid);
        if (TryComp<SpriteComponent>(pingUid, out var sprite))
            sprite.Color = color;

        var xform = Transform(pingUid);
        var originalCoords = xform.Coordinates;
        var mapId = xform.MapID;

        Robust.Client.Graphics.Texture? texture = null;
        if (sprite != null && sprite.BaseRSI != null && sprite[0].RsiState.IsValid)
        {
            if (sprite.BaseRSI.TryGetState(sprite[0].RsiState.Name, out var state))
                texture = state.Frame0;
        }

        var waypointData = new PingWaypointData(
            pingUid,
            pingComp.PingType,
            pingComp.Creator,
            pingComp.WorldPosition,
            originalCoords,
            mapId,
            color,
            texture,
            pingComp.DeleteAt
        )
        {
            AttachedTarget = pingComp.AttachedTarget,
            IsTargetValid = pingComp.AttachedTarget.HasValue,
            IsTilePing = !pingComp.AttachedTarget.HasValue,
            HasStoredPosition = true
        };

        _pingWaypoints[pingUid] = waypointData;
    }

    private Color GetColorFromEntity(EntityUid pingEntity)
    {
        if (TryComp<SpriteComponent>(pingEntity, out var sprite))
            return sprite.Color;
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

        var loadedPings = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<XenoPingEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var ping, out var xform))
        {
            if (!ShouldShowPing(ping.Creator))
                continue;

            loadedPings.Add(uid);

            if (!_pingWaypoints.TryGetValue(uid, out var waypointData))
                continue;

            UpdateWaypointFromPing(waypointData, ping, xform, uid);
        }

        CleanupUnloadedWaypoints(loadedPings);
    }

    private bool ShouldShowPing(EntityUid pingCreator)
    {
        var player = _playerManager.LocalEntity;
        if (player == null)
            return false;

        if (!HasComp<XenoComponent>(player))
            return false;

        if (!HasComp<HiveMemberComponent>(player))
            return false;

        if (!HasComp<HiveMemberComponent>(pingCreator))
            return false;

        var playerHive = _hive.GetHive(player.Value);
        var creatorHive = _hive.GetHive(pingCreator);

        if (playerHive == null || creatorHive == null)
            return false;

        return playerHive.Value.Owner == creatorHive.Value.Owner;
    }

    private void UpdateWaypointFromPing(PingWaypointData waypointData, XenoPingEntityComponent ping, TransformComponent xform, EntityUid uid)
    {
        waypointData.EntityIsLoaded = true;
        waypointData.WorldPosition = ping.WorldPosition;

        if (waypointData.IsTilePing && !waypointData.HasStoredPosition)
        {
            waypointData.OriginalCoordinates = xform.Coordinates;
            waypointData.HasStoredPosition = true;
        }

        waypointData.AttachedTarget = ping.AttachedTarget;
        waypointData.IsTargetValid = ping.AttachedTarget.HasValue;

        UpdateWaypointTexture(waypointData, uid);
    }

    private void UpdateWaypointTexture(PingWaypointData waypointData, EntityUid uid)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            if (sprite.Color != Color.White && sprite.Color != waypointData.Color)
                waypointData.Color = sprite.Color;

            if (waypointData.Texture == null && sprite.BaseRSI != null && sprite[0].RsiState.IsValid)
            {
                if (sprite.BaseRSI.TryGetState(sprite[0].RsiState.Name, out var state))
                    waypointData.Texture = state.Frame0;
            }
        }
    }

    private void CleanupUnloadedWaypoints(HashSet<EntityUid> loadedPings)
    {
        foreach (var (uid, waypointData) in _pingWaypoints.ToList())
        {
            if (loadedPings.Contains(uid))
                continue;

            if (!ShouldShowPing(waypointData.Creator))
            {
                _pingWaypoints.Remove(uid);
                continue;
            }

            waypointData.EntityIsLoaded = false;

            if (!EntityManager.EntityExists(uid))
            {
                _pingWaypoints.Remove(uid);
                continue;
            }

            if (TryComp<XenoPingEntityComponent>(uid, out var pingComp))
            {
                waypointData.AttachedTarget = pingComp.AttachedTarget;
                waypointData.WorldPosition = pingComp.WorldPosition;
                waypointData.IsTargetValid = pingComp.AttachedTarget.HasValue;
            }
        }
    }

    public override void Update(float frameTime)
    {
        var currentTime = _timing.CurTime;
        var toRemove = new List<EntityUid>();

        foreach (var (uid, data) in _pingWaypoints)
        {
            if (currentTime >= data.DeleteAt || !ShouldShowPing(data.Creator))
                toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
            _pingWaypoints.Remove(uid);
    }

    public IReadOnlyDictionary<EntityUid, PingWaypointData> GetPingWaypoints() => _pingWaypoints;

    public Dictionary<string, (string Name, Color Color, string Description)> GetAvailablePingTypesWithColors()
    {
        var basePings = GetAvailablePingTypes();
        var result = new Dictionary<string, (string Name, Color Color, string Description)>();

        foreach (var (entityId, (name, description)) in basePings)
        {
            var color = GetColorFromEntityPrototype(entityId);
            result[entityId] = (name, color, description);
        }

        return result;
    }

    private Color GetColorFromEntityPrototype(string entityId)
    {
        if (_prototypeManager.TryIndex<EntityPrototype>(entityId, out var entityProto) &&
            entityProto.Components.TryGetValue("Sprite", out var spriteComponent))
        {
            try
            {
                var spriteComp = (SpriteComponent)spriteComponent.Component;
                return spriteComp.Color;
            }
            catch
            {
            }
        }

        return Color.White;
    }

    public Dictionary<string, (string Name, Color Color, string Description)> GetAvailableConstructionPingTypesWithColors()
    {
        var basePings = GetAvailableConstructionPingTypes();
        var result = new Dictionary<string, (string Name, Color Color, string Description)>();

        foreach (var (entityId, (name, description)) in basePings)
        {
            var color = GetColorFromEntityPrototype(entityId);
            result[entityId] = (name, color, description);
        }

        return result;
    }

    public Dictionary<string, (string Name, Color Color, string Description)> GetPingsByCategoryWithColors(string category)
    {
        var basePings = GetPingsByCategory(category);
        var result = new Dictionary<string, (string Name, Color Color, string Description)>();

        foreach (var (entityId, (name, description)) in basePings)
        {
            var color = GetColorFromEntityPrototype(entityId);
            result[entityId] = (name, color, description);
        }

        return result;
    }
}
