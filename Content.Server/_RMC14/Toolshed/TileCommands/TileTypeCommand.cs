using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Commands.Entities;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._RMC14.Toolshed.TileCommands;

[ToolshedCommand, AdminCommand(AdminFlags.Query)]
internal sealed class TileTypeCommand : ToolshedCommand
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;


    [CommandImplementation("FromProtoId")]
    public IEnumerable<TileRef> TileType(
        [PipedArgument] IEnumerable<TileRef> input,
        string protoId,
        [CommandInverted] bool inverted
    )
    {
        ProtoId<ContentTileDefinition> tilePrototype = protoId;

        if (!_tileDefinition.TryGetDefinition(tilePrototype, out var tileDefinition))
            throw new ArgumentException($"Invalid tile definition {tilePrototype}.", nameof(protoId));
        return inverted
            ? input.Where(x => x.Tile.TypeId != tileDefinition.TileId)
            : input.Where(x => x.Tile.TypeId == tileDefinition.TileId);
    }

    [CommandImplementation("FromTileId")]
    public IEnumerable<TileRef> TileType(
        [PipedArgument] IEnumerable<TileRef> input,
        int id,
        [CommandInverted] bool inverted
    )
    {
        return inverted ? input.Where(x => x.Tile.TypeId != id) : input.Where(x => x.Tile.TypeId == id);
    }
}
