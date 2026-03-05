using Content.Shared._RMC14.Ping;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client._RMC14.Ping;

public abstract class RMCPingClientSystem<TPingEntityComponent, TPingDataComponent>
    : SharedRMCPingSystem<TPingEntityComponent, TPingDataComponent>
    where TPingEntityComponent : Component, RMCPingEntityComponent
    where TPingDataComponent : Component, RMCPingDataComponent
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<EntityUid, PingWaypointData> _pingWaypoints = new();

    protected EntityUid? LocalPlayer => _playerManager.LocalEntity;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TPingEntityComponent, ComponentInit>(OnPingEntityInit);
        SubscribeLocalEvent<TPingEntityComponent, ComponentShutdown>(OnPingEntityShutdown);
        SubscribeLocalEvent<TPingEntityComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _pingWaypoints.Clear();
    }

    private void OnPingEntityInit(Entity<TPingEntityComponent> ent, ref ComponentInit args)
    {
        if (TryComp<SpriteComponent>(ent.Owner, out var sprite))
            sprite.Visible = false;

        ProcessPingVisibility(ent.Owner);
    }

    private void OnAfterHandleState(Entity<TPingEntityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ProcessPingVisibility(ent.Owner);
    }

    private void ProcessPingVisibility(EntityUid pingUid)
    {
        if (!Exists(pingUid) || !TryComp<TPingEntityComponent>(pingUid, out var pingComp))
            return;

        if (pingComp.Creator == EntityUid.Invalid || !Exists(pingComp.Creator))
            return;

        var shouldShow = ShouldShowPing(pingComp.Creator);

        if (TryComp<SpriteComponent>(pingUid, out var spriteComp))
            spriteComp.Visible = shouldShow;

        if (!shouldShow || !ShouldCreateWaypoint(pingUid, pingComp))
            return;

        if (!_pingWaypoints.ContainsKey(pingUid))
            CreateWaypoint(pingUid, pingComp);
    }

    protected virtual bool ShouldCreateWaypoint(EntityUid pingUid, TPingEntityComponent pingComp)
    {
        return true;
    }

    private void CreateWaypoint(EntityUid pingUid, TPingEntityComponent pingComp)
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
            pingComp.DeleteAt)
        {
            AttachedTarget = pingComp.AttachedTarget,
            IsTargetValid = pingComp.AttachedTarget.HasValue,
            IsTilePing = !pingComp.AttachedTarget.HasValue,
            HasStoredPosition = true
        };

        OnWaypointCreated(pingUid, pingComp, waypointData);
        _pingWaypoints[pingUid] = waypointData;
    }

    protected virtual void OnWaypointCreated(EntityUid pingUid, TPingEntityComponent pingComp, PingWaypointData waypointData)
    {
    }

    private Color GetColorFromEntity(EntityUid pingEntity)
    {
        if (TryComp<SpriteComponent>(pingEntity, out var sprite))
            return sprite.Color;

        return Color.White;
    }

    private void OnPingEntityShutdown(Entity<TPingEntityComponent> ent, ref ComponentShutdown args)
    {
        _pingWaypoints.Remove(ent.Owner);
    }

    public sealed override void FrameUpdate(float frameTime)
    {
        if (LocalPlayer == null)
            return;

        var loadedPings = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<TPingEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var ping, out var xform))
        {
            if (!ShouldShowPing(ping.Creator))
                continue;

            loadedPings.Add(uid);

            if (!_pingWaypoints.TryGetValue(uid, out var waypointData))
            {
                if (ShouldCreateWaypoint(uid, ping))
                    CreateWaypoint(uid, ping);

                continue;
            }

            UpdateWaypointFromPing(waypointData, ping, xform, uid);
        }

        CleanupUnloadedWaypoints(loadedPings);
    }

    protected virtual void UpdateWaypointFromPing(
        PingWaypointData waypointData,
        TPingEntityComponent ping,
        TransformComponent xform,
        EntityUid uid)
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
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.Color != Color.White && sprite.Color != waypointData.Color)
            waypointData.Color = sprite.Color;

        if (waypointData.Texture == null && sprite.BaseRSI != null && sprite[0].RsiState.IsValid)
        {
            if (sprite.BaseRSI.TryGetState(sprite[0].RsiState.Name, out var state))
                waypointData.Texture = state.Frame0;
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

            if (TryComp<TPingEntityComponent>(uid, out var pingComp))
            {
                waypointData.AttachedTarget = pingComp.AttachedTarget;
                waypointData.WorldPosition = pingComp.WorldPosition;
                waypointData.IsTargetValid = pingComp.AttachedTarget.HasValue;
            }
        }
    }

    public sealed override void Update(float frameTime)
    {
        var currentTime = _timing.CurTime;
        var toRemove = new List<EntityUid>();

        foreach (var (uid, data) in _pingWaypoints)
        {
            if (currentTime >= data.DeleteAt || !ShouldShowPing(data.Creator))
                toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            _pingWaypoints.Remove(uid);
        }
    }

    protected abstract bool ShouldShowPing(EntityUid pingCreator);

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

    private Color GetColorFromEntityPrototype(string entityId)
    {
        if (_prototypeManager.TryIndex<EntityPrototype>(entityId, out var entityProto) &&
            entityProto.Components.TryGetValue("Sprite", out var spriteComponent))
        {
            try
            {
                var spriteComp = (SpriteComponent) spriteComponent.Component;
                return spriteComp.Color;
            }
            catch
            {
            }
        }

        return Color.White;
    }
}
