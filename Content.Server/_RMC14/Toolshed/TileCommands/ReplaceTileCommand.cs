using System.Linq;
using Content.Server.Administration;
using Content.Server.Power.Nodes;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Commands.Entities;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._RMC14.Toolshed.TileCommands;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
internal sealed class ReplaceTileCommand : ToolshedCommand
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    private MapSystem? _mapSys;

    [CommandImplementation("FromProtoId")]
    public void ReplaceTile(
        [PipedArgument] IEnumerable<TileRef> input,
        string protoId,
        [CommandInverted] bool inverted
    )
    {
        _mapSys ??= Sys<MapSystem>();
        ProtoId<ContentTileDefinition> tilePrototype = protoId;

        if (!_tileDefinition.TryGetDefinition(tilePrototype, out var tileDefinition))
            throw new ArgumentException($"Invalid tile definition {tilePrototype}.", nameof(protoId));

        foreach (var tileRef in input)
        {
            if (!TryComp<MapGridComponent>(tileRef.GridUid, out var mapGrid))
                throw new ArgumentException($"Invalid tile reference {tileRef.GridUid}.", nameof(input));
            _mapSys.SetTile(tileRef.GridUid, mapGrid, tileRef.GridIndices, new Tile(tileDefinition.TileId));
        }
    }

    [CommandImplementation("FromTileId")]
    public void ReplaceTile(
        [PipedArgument] IEnumerable<TileRef> input,
        int id,
        [CommandInverted] bool inverted
    )
    {
        _mapSys ??= Sys<MapSystem>();

        foreach (var tileRef in input)
        {
            if (!TryComp<MapGridComponent>(tileRef.GridUid, out var mapGrid))
                throw new ArgumentException($"Invalid tile reference {tileRef.GridUid}.", nameof(input));
            _mapSys.SetTile(tileRef.GridUid, mapGrid, tileRef.GridIndices, new Tile(id));
        }
    }
}
