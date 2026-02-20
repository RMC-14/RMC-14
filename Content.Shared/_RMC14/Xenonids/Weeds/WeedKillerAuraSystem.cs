using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.ResinSurge;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Weeds;

/// <summary>
/// System that manages weed killer auras around entities.
/// Creates invisible anchored entities with BlockWeedsComponent in a radius.
/// Automatically updates blocker positions when parent entity moves.
/// Also destroys existing weeds and resin walls in the radius.
/// </summary>
public sealed class WeedKillerAuraSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;

    private EntityQuery<XenoWeedsComponent> _weedsQuery;
    private EntityQuery<ResinSurgeReinforcableComponent> _resinWallQuery;

    public override void Initialize()
    {
        base.Initialize();
        
        _weedsQuery = GetEntityQuery<XenoWeedsComponent>();
        _resinWallQuery = GetEntityQuery<ResinSurgeReinforcableComponent>();
        
        SubscribeLocalEvent<WeedKillerAuraComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WeedKillerAuraComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WeedKillerAuraComponent, MoveEvent>(OnParentMoved);
    }

    private void OnMapInit(Entity<WeedKillerAuraComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        var blocker = ent.Comp;

        // Calculate expiration time if duration is set
        if (blocker.BlockDuration > TimeSpan.Zero && blocker.ExpireAt == TimeSpan.Zero)
        {
            blocker.ExpireAt = _timing.CurTime + blocker.BlockDuration;
            Dirty(ent, blocker);
        }

        // Destroy existing weeds and resin walls in the radius
        DestroyWeedsInRadius(ent);

        // Spawn weed blocker entities in a radius around this entity
        CreateWeedBlockers(ent);
    }

    private void OnParentMoved(Entity<WeedKillerAuraComponent> ent, ref MoveEvent args)
    {
        if (_net.IsClient)
            return;

        if (!ent.Comp.Active)
            return;

        // Only update if we actually moved to a different tile or grid
        Vector2i oldTile, newTile;
        var oldGridUid = args.OldPosition.EntityId;
        var newGridUid = args.NewPosition.EntityId;

        if (TryComp<MapGridComponent>(oldGridUid, out var oldGrid) &&
            TryComp<MapGridComponent>(newGridUid, out var newGrid))
        {
            oldTile = _mapSystem.CoordinatesToTile(oldGridUid, oldGrid, args.OldPosition);
            newTile = _mapSystem.CoordinatesToTile(newGridUid, newGrid, args.NewPosition);

            if (oldGridUid == newGridUid && oldTile == newTile)
                return;
        }

        // Recreate the blocker entities at the new position
        RemoveWeedBlockers(ent);
        DestroyWeedsInRadius(ent);
        CreateWeedBlockers(ent);
    }

    private void OnShutdown(Entity<WeedKillerAuraComponent> ent, ref ComponentShutdown args)
    {
        // Clean up blocker entities when component is removed or entity is deleted
        RemoveWeedBlockers(ent);
    }

    private void CreateWeedBlockers(Entity<WeedKillerAuraComponent> ent)
    {
        var gridId = _transform.GetGrid(ent.Owner);
        if (gridId == null || !TryComp<MapGridComponent>(gridId, out var grid))
        {
            Log.Warning($"Entity {ToPrettyString(ent.Owner)} has no grid, cannot create weed blocker aura");
            return;
        }

        var entityPos = _transform.GetGridOrMapTilePosition(ent.Owner);
        var radius = ent.Comp.BlockRadius;

        // Create a list to track spawned blockers
        var blockerList = new List<EntityUid>();

        // Spawn blocker entities in a square radius
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var targetTile = entityPos + new Vector2i(x, y);
                var coords = _mapSystem.GridTileToLocal(gridId.Value, grid, targetTile);

                // Spawn an invisible weed blocker entity
                var blocker = Spawn(null, coords);
                EnsureComp<BlockWeedsComponent>(blocker);
                
                // CRITICAL: Anchor the blocker to the grid so it can be detected by weed system
                var xform = Transform(blocker);
                _transform.AnchorEntity(blocker, xform, gridId.Value, grid, targetTile);
                
                blockerList.Add(blocker);
            }
        }

        // Store the blocker entities so we can remove them later
        ent.Comp.BlockerEntities = blockerList;
        Dirty(ent, ent.Comp);
    }

    private void RemoveWeedBlockers(Entity<WeedKillerAuraComponent> ent)
    {
        foreach (var blockerEntity in ent.Comp.BlockerEntities)
        {
            QueueDel(blockerEntity);
        }

        ent.Comp.BlockerEntities.Clear();
    }

    private void DestroyWeedsInRadius(Entity<WeedKillerAuraComponent> ent)
    {
        var gridId = _transform.GetGrid(ent.Owner);
        if (gridId == null || !TryComp<MapGridComponent>(gridId, out var grid))
            return;

        var entityPos = _transform.GetGridOrMapTilePosition(ent.Owner);
        var radius = ent.Comp.BlockRadius;

        // Find and destroy all weeds and resin walls in the radius
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var targetTile = entityPos + new Vector2i(x, y);
                var anchored = _rmcMap.GetAnchoredEntitiesEnumerator((gridId.Value, grid), targetTile);
                
                while (anchored.MoveNext(out var anchoredId))
                {
                    // Check if this is a weed entity
                    if (_weedsQuery.HasComp(anchoredId))
                    {
                        QueueDel(anchoredId);
                    }
                    // Check if this is a resin wall
                    else if (_resinWallQuery.HasComp(anchoredId))
                    {
                        QueueDel(anchoredId);
                    }
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        // Handle weed blocker expiration
        var blockerQuery = EntityQueryEnumerator<WeedKillerAuraComponent>();
        while (blockerQuery.MoveNext(out var blockerId, out var blocker))
        {
            if (!blocker.Active || blocker.ExpireAt == TimeSpan.Zero)
                continue;

            if (time >= blocker.ExpireAt)
            {
                blocker.Active = false;
                Dirty(blockerId, blocker);

                // Remove all blocker entities
                RemoveWeedBlockers((blockerId, blocker));

                // Notify listeners that the aura expired naturally (component remains)
                RaiseLocalEvent(blockerId, new WeedKillerAuraExpiredEvent());
            }
        }
    }
}
