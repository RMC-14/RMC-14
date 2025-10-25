using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Toolshed.TileCommands;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
internal sealed class NearbyTileCommand : ToolshedCommand
{
    private MapSystem? _mapSys;

    [CommandImplementation]
    public IEnumerable<TileRef> NearbyTile([PipedArgument] IEnumerable<EntityUid> input, float range)
    {
        _mapSys ??= Sys<MapSystem>();

        foreach (var entityId in input)
        {
            var xform = EntityManager.GetComponent<TransformComponent>(entityId);

            var playerGrid = xform.GridUid;

            if (!EntityManager.TryGetComponent<MapGridComponent>(playerGrid, out var mapGrid))
                throw new ArgumentException("Missing map grid component from gridUid from the specified entity!");

            var playerPosition = xform.Coordinates;

            for (var i = -range; i <= range; i++)
            {
                for (var j = -range; j <= range; j++)
                {
                    var tile = _mapSys.GetTileRef(playerGrid.Value,
                        mapGrid,
                        playerPosition.Offset(new Vector2(i, j)));
                    yield return tile;
                }
            }
        }
    }
}
