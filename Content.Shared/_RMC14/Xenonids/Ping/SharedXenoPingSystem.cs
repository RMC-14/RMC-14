using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Content.Shared._RMC14.Xenonids.Ping;

public abstract class SharedXenoPingSystem : EntitySystem
{
    [Dependency] protected readonly SharedXenoHiveSystem _hive = default!;
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
        UpdateAttachedTargetPositions(currentTime);
        CleanupExpiredPings(currentTime);
    }

    private void UpdateAttachedTargetPositions(TimeSpan currentTime)
    {
        var pingsToUpdate = new List<(EntityUid uid, XenoPingEntityComponent ping)>();

        var query = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (ping.AttachedTarget.HasValue)
            {
                pingsToUpdate.Add((uid, ping));
            }
        }

        foreach (var (uid, ping) in pingsToUpdate)
        {
            UpdateSinglePingPosition(uid, ping);
        }
    }

    private void UpdateSinglePingPosition(EntityUid pingUid, XenoPingEntityComponent ping)
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

        var targetWorldPos = _transform.GetWorldPosition(targetEntity);
        var currentWorldPos = _transform.GetWorldPosition(pingUid);
        var distanceMoved = Vector2.Distance(targetWorldPos, currentWorldPos);

        if (distanceMoved > AttachedTargetPositionThreshold)
        {
            UpdatePingPosition(pingUid, ping, targetTransform.Coordinates, targetWorldPos);
        }
    }

    private void HandleDetachedTarget(EntityUid pingUid, XenoPingEntityComponent ping)
    {
        if (ping.LastKnownCoordinates.HasValue)
        {
            _transform.SetCoordinates(pingUid, ping.LastKnownCoordinates.Value);
        }

        ping.AttachedTarget = null;
        Dirty(pingUid, ping);
    }

    private void UpdatePingPosition(EntityUid pingUid, XenoPingEntityComponent ping, EntityCoordinates targetCoordinates, Vector2 targetWorldPos)
    {
        ping.LastKnownCoordinates = targetCoordinates;
        ping.WorldPosition = targetWorldPos;
        _transform.SetCoordinates(pingUid, targetCoordinates);
        Dirty(pingUid, ping);
    }

    private void CleanupExpiredPings(TimeSpan currentTime)
    {
        var toDelete = new List<EntityUid>();

        var query = EntityQueryEnumerator<XenoPingEntityComponent>();
        while (query.MoveNext(out var uid, out var ping))
        {
            if (currentTime >= ping.DeleteAt)
            {
                toDelete.Add(uid);
            }
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

    protected EntityCoordinates GetCoordinates(NetCoordinates netCoordinates)
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
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            if (!pingData.IsConstruction)
            {
                result[prototype.ID] = (pingData.Name, pingData.Description);
            }
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
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            if (pingData.IsConstruction)
            {
                result[prototype.ID] = (pingData.Name, pingData.Description);
            }
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
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            if (pingData.Categories.Contains(category))
            {
                result[prototype.ID] = (pingData.Name, pingData.Description);
            }
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
            if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
                continue;

            var pingData = (XenoPingDataComponent)pingDataComponent.Component;
            foreach (var category in pingData.Categories)
            {
                categories.Add(category);
            }
        }

        return categories.OrderBy(x => x);
    }

    private XenoPingDataComponent? GetPingDataFromEntityId(string entityId)
    {
        if (!_prototypeManager.TryIndex<EntityPrototype>(entityId, out var prototype))
            return null;

        if (!prototype.Components.TryGetValue("XenoPingData", out var pingDataComponent))
            return null;

        return (XenoPingDataComponent)pingDataComponent.Component;
    }
}
