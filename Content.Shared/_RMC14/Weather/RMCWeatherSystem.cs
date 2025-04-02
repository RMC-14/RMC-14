using Content.Shared._RMC14.Areas;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Weather;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weather;

public sealed class RMCWeatherSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roof = default!;
    [Dependency] private readonly AreaSystem _area = default!;

    private EntityQuery<BlockWeatherComponent> _blockQuery;

    public override void Initialize()
    {
        base.Initialize();
        _blockQuery = GetEntityQuery<BlockWeatherComponent>();
    }

    public bool CanWeatherAffectArea(EntityUid uid, MapGridComponent grid, TileRef tileRef, RoofComponent? roofComp = null)
    {
        if (!TryComp<AreaGridComponent>(uid, out var areaGridComp) && tileRef.Tile.IsEmpty)
            return true;

        if (Resolve(uid, ref roofComp, false) && _roof.IsRooved((uid, grid, roofComp), tileRef.GridIndices))
            return false;

        //RMC14 - Check if the area has weather enabled
        if (!_area.IsWeatherEnabled((uid, grid), tileRef.GridIndices))
            return false;

        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];

        // if (!tileDef.Weather)
        //     return false;

        var anchoredEntities = _mapSystem.GetAnchoredEntitiesEnumerator(uid, grid, tileRef.GridIndices);

        while (anchoredEntities.MoveNext(out var ent))
        {
            if (_blockQuery.HasComponent(ent.Value))
                return false;
        }

        return true;

    }
}
