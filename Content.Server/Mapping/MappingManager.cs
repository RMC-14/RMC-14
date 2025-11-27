using System.IO;
using Content.Server.Administration.Managers;
using Content.Server.Decals;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Content.Shared.Mapping;
using Robust.Server.Player;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Mapping;

public sealed class MappingManager : IPostInjectInit
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IEntityManager _ent = default!;

    private ISawmill _sawmill = default!;
    private ZStdCompressionContext _zstd = default!;

    // RMC14
    [Dependency] private readonly IMapManager _mapManager = default!;
    // RMC14

    public void PostInject()
    {
#if !FULL_RELEASE
        _net.RegisterNetMessage<MappingSaveMapMessage>(OnMappingSaveMap);
        _net.RegisterNetMessage<MappingSaveMapErrorMessage>();
        _net.RegisterNetMessage<MappingMapDataMessage>();
        _net.RegisterNetMessage<MappingDragGrabMessage>(OnMappingDragGrab);

        _sawmill = _log.GetSawmill("mapping");
        _zstd = new ZStdCompressionContext();
#endif
    }

    private void OnMappingSaveMap(MappingSaveMapMessage message)
    {
#if !FULL_RELEASE
        try
        {
            if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
                !_admin.IsAdmin(session, true) ||
                !_admin.HasAdminFlag(session, AdminFlags.Host) ||
                !_ent.TryGetComponent(session.AttachedEntity, out TransformComponent? xform) ||
                xform.MapUid is not {} mapUid)
            {
                return;
            }

            var sys = _systems.GetEntitySystem<MapLoaderSystem>();
            var data = sys.SerializeEntitiesRecursive([mapUid]).Node;
            var document = new YamlDocument(data.ToYaml());
            var stream = new YamlStream { document };
            var writer = new StringWriter();
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);

            var msg = new MappingMapDataMessage()
            {
                Context = _zstd,
                Yml = writer.ToString()
            };
            _net.ServerSendMessage(msg, message.MsgChannel);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error saving map in mapping mode:\n{e}");
            var msg = new MappingSaveMapErrorMessage();
            _net.ServerSendMessage(msg, message.MsgChannel);
        }
#endif
    }

    private void OnMappingDragGrab(MappingDragGrabMessage message)
    {
        var box = message.Box;
        var bottomLeft = new MapCoordinates(box.BottomLeft, message.Map);
        var map = _ent.System<SharedMapSystem>();
        if (!_mapManager.TryFindGridAt(bottomLeft, out var gridId, out var grid))
            return;

        var decalsSystem = _ent.System<DecalSystem>();
        var decals = new List<Decal>();
        var boxShrank = box.Enlarged(-0.05f);
        foreach (var decal in decalsSystem.GetDecalsIntersecting(gridId, boxShrank))
        {
            decals.Add(decal.Decal);
        }

        var moveTiles = new List<(Vector2i Position, Tile Tile)>();
        var spaceTiles = new List<(Vector2i Position, Tile Tile)>();
        foreach (var tile in map.GetTilesIntersecting(gridId, grid, box, false))
        {
            var targetPosition = tile.GridIndices + message.Offset.Floored();
            moveTiles.Add((targetPosition, tile.Tile));
            spaceTiles.Add((tile.GridIndices, Tile.Empty));
        }

        if (message.SpaceSourceTiles)
            map.SetTiles(gridId, grid, spaceTiles);

        map.SetTiles(gridId, grid, moveTiles);

        var lookup = _ent.System<EntityLookupSystem>();
        var transform = _ent.System<SharedTransformSystem>();
        foreach (var entity in lookup.GetEntitiesIntersecting(message.Map, boxShrank, LookupFlags.Uncontained))
        {
            if (_ent.HasComponent<ActorComponent>(entity))
                continue;

            if (!_ent.TryGetComponent(entity, out TransformComponent? xform))
                continue;

            var anchored = xform.Anchored;
            transform.SetCoordinates(entity, xform.Coordinates.Offset(message.Offset));

            if (anchored)
                transform.AnchorEntity(entity);
        }

        foreach (var decal in decals)
        {
            decalsSystem.TryAddDecal(decal, new EntityCoordinates(gridId, decal.Coordinates + message.Offset), out _);
        }
    }
}
