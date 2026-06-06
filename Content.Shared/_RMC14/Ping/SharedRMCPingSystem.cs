using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace Content.Shared._RMC14.Ping;

public abstract class SharedRMCPingSystem<TPingEntityComponent, TPingDataComponent> : EntitySystem
    where TPingEntityComponent : Component, RMCPingEntityComponent
    where TPingDataComponent : Component, RMCPingDataComponent
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private const float AttachedTargetPositionThreshold = 0.01f;

    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _transformQuery = GetEntityQuery<TransformComponent>();
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var currentTime = _timing.CurTime;
        UpdateAttachedTargetPositions();
        CleanupExpiredPings(currentTime);
    }

    private void UpdateAttachedTargetPositions()
    {
        var pingsToUpdate = new List<(EntityUid uid, TPingEntityComponent ping)>();

        var query = EntityQueryEnumerator<TPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (ping.AttachedTarget.HasValue)
                pingsToUpdate.Add((uid, ping));
        }

        foreach (var (uid, ping) in pingsToUpdate)
        {
            UpdateSinglePingPosition(uid, ping);
        }
    }

    private void UpdateSinglePingPosition(EntityUid pingUid, TPingEntityComponent ping)
    {
        if (!ping.AttachedTarget.HasValue)
            return;

        var targetEntity = ping.AttachedTarget.Value;

        if (!Exists(targetEntity))
        {
            HandleDetachedTarget(pingUid, ping);
            return;
        }

        if (!_transformQuery.TryGetComponent(targetEntity, out var targetTransform))
            return;

        var targetCoordinates = targetTransform.Coordinates;
        var displayCoordinates = targetCoordinates.Offset(ping.AttachedOffset);
        var displayWorldPos = _transform.ToMapCoordinates(displayCoordinates).Position;
        var currentWorldPos = _transform.GetWorldPosition(pingUid);
        var distanceMoved = Vector2.Distance(displayWorldPos, currentWorldPos);

        if (distanceMoved > AttachedTargetPositionThreshold)
        {
            UpdatePingPosition(pingUid, ping, targetCoordinates, displayCoordinates, displayWorldPos);
        }
    }

    private void HandleDetachedTarget(EntityUid pingUid, TPingEntityComponent ping)
    {
        if (ping.LastKnownCoordinates.HasValue)
        {
            var displayCoordinates = ping.LastKnownCoordinates.Value.Offset(ping.AttachedOffset);
            _transform.SetCoordinates(pingUid, displayCoordinates);
            ping.WorldPosition = _transform.ToMapCoordinates(displayCoordinates).Position;
        }

        ping.AttachedTarget = null;
        Dirty(pingUid, ping);
    }

    private void UpdatePingPosition(
        EntityUid pingUid,
        TPingEntityComponent ping,
        EntityCoordinates targetCoordinates,
        EntityCoordinates displayCoordinates,
        Vector2 displayWorldPos)
    {
        ping.LastKnownCoordinates = targetCoordinates;
        ping.WorldPosition = displayWorldPos;
        _transform.SetCoordinates(pingUid, displayCoordinates);
        Dirty(pingUid, ping);
    }

    private void CleanupExpiredPings(TimeSpan currentTime)
    {
        var toDelete = new List<EntityUid>();

        var query = EntityQueryEnumerator<TPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (currentTime >= ping.DeleteAt)
                toDelete.Add(uid);
        }

        foreach (var uid in toDelete)
        {
            QueueDel(uid);
        }
    }

    protected bool IsValidCoordinates(EntityCoordinates coordinates)
    {
        return coordinates.IsValid(EntityManager) && _mapManager.MapExists(coordinates.GetMapId(EntityManager));
    }

    protected EntityUid? ResolveTargetEntity(NetEntity? targetNetEntity)
    {
        if (!targetNetEntity.HasValue)
            return null;

        return TryGetEntity(targetNetEntity.Value, out var entityUid) ? entityUid : null;
    }

    protected new EntityCoordinates GetCoordinates(NetCoordinates netCoordinates)
    {
        if (!TryGetEntity(netCoordinates.NetEntity, out var entityUid) || entityUid == null)
            return EntityCoordinates.Invalid;

        return new EntityCoordinates(entityUid.Value, netCoordinates.Position);
    }

    public Dictionary<string, (string Name, string Description)> GetAvailablePingTypes()
    {
        var result = new Dictionary<string, (string Name, string Description)>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!TryGetPingData(prototype, out var pingData))
                continue;

            if (!pingData.IsConstruction)
                result[prototype.ID] = (pingData.Name, pingData.Description);
        }

        return result
            .OrderByDescending(x => GetPingDataFromEntityId(x.Key)?.Priority ?? 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public Dictionary<string, (string Name, string Description)> GetAvailableConstructionPingTypes()
    {
        var result = new Dictionary<string, (string Name, string Description)>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!TryGetPingData(prototype, out var pingData))
                continue;

            if (pingData.IsConstruction)
                result[prototype.ID] = (pingData.Name, pingData.Description);
        }

        return result
            .OrderByDescending(x => GetPingDataFromEntityId(x.Key)?.Priority ?? 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public Dictionary<string, (string Name, string Description)> GetPingsByCategory(string category)
    {
        var result = new Dictionary<string, (string Name, string Description)>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!TryGetPingData(prototype, out var pingData))
                continue;

            if (pingData.Categories.Contains(category))
                result[prototype.ID] = (pingData.Name, pingData.Description);
        }

        return result
            .OrderByDescending(x => GetPingDataFromEntityId(x.Key)?.Priority ?? 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public IEnumerable<string> GetAvailableCategories()
    {
        var categories = new HashSet<string>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (!TryGetPingData(prototype, out var pingData))
                continue;

            foreach (var category in pingData.Categories)
            {
                categories.Add(category);
            }
        }

        return categories.OrderBy(x => x);
    }

    private TPingDataComponent? GetPingDataFromEntityId(string entityId)
    {
        if (!_prototypeManager.TryIndex<EntityPrototype>(entityId, out var prototype))
            return null;

        return TryGetPingData(prototype, out var pingData) ? pingData : null;
    }

    private static bool TryGetPingData(
        EntityPrototype prototype,
        [NotNullWhen(true)] out TPingDataComponent? pingData)
    {
        foreach (var component in prototype.Components.Values)
        {
            if (component.Component is TPingDataComponent typedData)
            {
                pingData = typedData;
                return true;
            }
        }

        pingData = null;
        return false;
    }
}
