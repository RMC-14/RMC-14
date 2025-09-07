using System.Numerics;
using Content.Server.Shuttles.Events;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Shuttles;
using Content.Shared.Shuttles.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server._RMC14.Shuttles;

public sealed class RMCShuttleSystem : SharedRMCShuttleSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlaySoundOnFTLStartComponent, FTLStartedEvent>(OnPlaySoundOnFTLStart);

        SubscribeLocalEvent<RMCSpawnEntityOnFTLStartComponent, BeforeFTLStartedEvent>(BeforeFTLStarted);
        SubscribeLocalEvent<RMCSpawnEntityOnFTLStartComponent, FTLStartedEvent>(OnSpawnEntityOnFTLStart);

        SubscribeLocalEvent<FTLComponent, BeforeFTLFinishedEvent>(BeforeFTLFinished);
    }

    private void OnPlaySoundOnFTLStart(Entity<PlaySoundOnFTLStartComponent> ent, ref FTLStartedEvent args)
    {
        if (Transform(ent).GridUid is not { } grid)
            return;

        _audio.PlayPvs(ent.Comp.Sound, grid);
        RemCompDeferred<PlaySoundOnFTLStartComponent>(ent);
    }

    /// <summary>
    ///     Spawn an entity on every tile that the FTLing grid occupied after it has moved to FTL space.
    /// </summary>
    private void OnSpawnEntityOnFTLStart(Entity<RMCSpawnEntityOnFTLStartComponent> ent, ref FTLStartedEvent args)
    {
        foreach (var coordinate in ent.Comp.Coordinates)
        {
            Spawn(ent.Comp.SpawnedEntity, coordinate);
        }
    }

    /// <summary>
    ///     Get the MapCoordinates for every tile that the grid about to FTL is occupying.
    /// </summary>
    private void BeforeFTLStarted(Entity<RMCSpawnEntityOnFTLStartComponent> ent, ref BeforeFTLStartedEvent args)
    {
        if (!TryComp(ent, out MapGridComponent? grid))
            return;

        var enumerator = _mapSystem.GetAllTilesEnumerator(ent, grid);
        while (enumerator.MoveNext(out var tile))
        {
            if(!TryComp(ent, out MapGridComponent? mapGrid))
                return;

            var mapCoords = _mapSystem.GridTileToWorld(ent, mapGrid, tile.Value.GridIndices);
            ent.Comp.Coordinates.Add(mapCoords);
        }
    }

    private void BeforeFTLFinished(Entity<FTLComponent> ent, ref BeforeFTLFinishedEvent args)
    {
        var mapGrid = Transform(ent.Comp.TargetCoordinates.EntityId).GridUid;
        if (mapGrid == null)
            return;

        // Create a box that has the width and height of the FTLing grid.
        var shuttleAABB = Comp<MapGridComponent>(ent).LocalAABB;
        var shuttleHeight = (float)Math.Floor(shuttleAABB.Height / 2f);
        var shuttleWidth = (float)Math.Floor(shuttleAABB.Width / 2f);
        var expansionHeight = shuttleAABB.Height % 2 == 0 ? shuttleHeight - 1 : shuttleHeight;
        var expansionWidth = shuttleAABB.Width % 2 == 0 ? shuttleWidth - 1 : shuttleWidth;

        // Center the box around the destination.
        var targetLocalAABB = Box2.CenteredAround(ent.Comp.TargetCoordinates.Position, Vector2.One);
        var extinguishArea = new Box2(targetLocalAABB.Left - expansionWidth,
            targetLocalAABB.Bottom - expansionHeight,
            targetLocalAABB.Right + expansionWidth,
            targetLocalAABB.Top + expansionHeight);
        var targetLocalAABBExpanded = _transform.GetWorldMatrix(Transform(mapGrid.Value)).TransformBox(extinguishArea);

        // Delete all tile fires inside the box.
        var lookupEntities = new HashSet<EntityUid>();
        _lookup.GetLocalEntitiesIntersecting(mapGrid.Value, targetLocalAABBExpanded, lookupEntities, LookupFlags.Uncontained);

        foreach (var entity in lookupEntities)
        {
            if (HasComp<TileFireComponent>(entity))
                Del(entity);
        }
    }
}

[ByRefEvent]
public record struct BeforeFTLStartedEvent;

[ByRefEvent]
public record struct BeforeFTLFinishedEvent;
