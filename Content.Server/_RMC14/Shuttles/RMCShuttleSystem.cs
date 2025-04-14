using Content.Server.Shuttles.Events;
using Content.Shared._RMC14.Shuttles;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Server._RMC14.Shuttles;

public sealed class RMCShuttleSystem : SharedRMCShuttleSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlaySoundOnFTLStartComponent, FTLStartedEvent>(OnPlaySoundOnFTLStart);

        SubscribeLocalEvent<RMCSpawnEntityOnFTLStartComponent, BeforeFTLStartedEvent>(BeforeFTLStarted);
        SubscribeLocalEvent<RMCSpawnEntityOnFTLStartComponent, FTLStartedEvent>(OnSpawnEntityOnFTLStart);
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
        var uid = args.FTLEntity;
        var manager = args.Manager;
        var grid = args.Grid;
        var xform = args.Xform;

        if (!Resolve(uid, ref manager, ref grid, ref xform) || xform.MapUid == null)
            return;

        var tiles = new HashSet<Vector2i>();
        if (TryComp(uid, out MapGridComponent? shuttleGrid))
        {
            var enumerator = _mapSystem.GetAllTilesEnumerator(uid, shuttleGrid);
            while (enumerator.MoveNext(out var tile))
            {
                tiles.Add(tile.Value.GridIndices);
            }
        }

        foreach (var fixture in manager.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            foreach (var tile in tiles)
            {
                if(!TryComp(uid, out MapGridComponent? mapGrid))
                    return;

                var mapCoords = _mapSystem.GridTileToWorld(uid, mapGrid, tile);
                ent.Comp.Coordinates.Add(mapCoords);
            }
        }
    }
}

[ByRefEvent]
public record struct BeforeFTLStartedEvent(EntityUid FTLEntity, FixturesComponent? Manager = null, MapGridComponent? Grid = null, TransformComponent? Xform = null);
