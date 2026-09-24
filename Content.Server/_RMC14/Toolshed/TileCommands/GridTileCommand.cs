using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Toolshed.TileCommands;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
internal sealed class GridTileCommand : ToolshedCommand
{
    private MapSystem? _mapSys;

    [CommandImplementation]
    public IEnumerable<TileRef> GridTile([PipedArgument] EntityUid input)
    {
        _mapSys ??= Sys<MapSystem>();

        var xform = EntityManager.GetComponent<TransformComponent>(input);

        var playerGrid = xform.GridUid;

        if (!EntityManager.TryGetComponent<MapGridComponent>(playerGrid, out var mapGrid))
            throw new ArgumentException("Missing map grid component from gridUid from the specified entity!");

        var tile = _mapSys.GetAllTiles(playerGrid.Value, mapGrid);
        return tile;
    }
}
