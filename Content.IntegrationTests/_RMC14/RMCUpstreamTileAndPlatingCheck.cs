using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Content.Server.Engineering.Components;
using Content.Shared.Maps;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests._RMC14;

[TestFixture]
public sealed class RMCUpstreamTileAndPlatingCheck
{
    private readonly ProtoId<ContentTileDefinition> _tilePrototypeId = "Plating";

    private static List<string> FileFetch()
    {
        var rootDir = Path.Join(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.ToString());
        var relativePath = Path.Combine(rootDir, "Resources");
        var filesDir = Path.Combine(relativePath, "Maps", "_RMC14");

        try
        {
            var relativeFiles = new List<string>();
            var files = Directory.GetFiles(filesDir, "*.yml", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                relativeFiles.Add(Path.GetRelativePath(relativePath, file));
            }

            return relativeFiles;
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"Directory {filesDir} does not exist");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Access to directory {filesDir} is denied");
        }

        return [];
    }

    [Test]
    public async Task RMCPlatingCheck()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var sMapSystem = server.System<SharedMapSystem>();
        var sMapLoaderSystem = server.System<MapLoaderSystem>();
        var sTileSystem = server.System<TileSystem>();
        var siTileDefinitionManager = server.Resolve<ITileDefinitionManager>();
        if (!siTileDefinitionManager.TryGetDefinition(_tilePrototypeId, out var tileDefinition))
            return;

        var deSerOpts = new DeserializationOptions();
        deSerOpts.LogOrphanedGrids = false;

        var mapLoadOpts = new MapLoadOptions();
        mapLoadOpts.DeserializationOptions = deSerOpts;

        var files = FileFetch();
        await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                    {
                        foreach (var file in files)
                        {
                            var tileErrorsBefore = new HashSet<string>();
                            var tileErrorsAfter = new HashSet<string>();
                            sMapLoaderSystem.TryLoadGeneric(new ResPath(file), out var map, out var grids, mapLoadOpts);
                            if (grids == null)
                                continue;
                            foreach (var grid in grids)
                            {
                                var allTiles = sMapSystem.GetAllTiles(grid, grid.Comp);
                                foreach (var tile in allTiles)
                                {
                                    if (tile.Tile.TypeId == tileDefinition.TileId)
                                    {
                                        tileErrorsBefore.Add(tile.GridIndices.ToString());
                                        continue;
                                    }

                                    sTileSystem.PryTile(tile);
                                    if (!sMapSystem.TryGetTile(grid, tile.GridIndices, out var priedTile))
                                        continue;

                                    if (priedTile.TypeId == tileDefinition.TileId)
                                    {
                                        tileErrorsAfter.Add($"{tile.GridIndices.ToString()}, {tile.Tile.TypeId}");
                                    }
                                }
                            }

                            if (tileErrorsBefore.Count == 0 && tileErrorsAfter.Count == 0)
                                continue;

                            var msg = $"For {file} found:";
                            if (tileErrorsBefore.Count > 0)
                            {
                                msg +=
                                    ("\nUpstream Plating was used (use [self gridtile tiletype:FromProtoId \"Plating\" replacetile:FromProtoId \"CMFloorPlating\"] over the grid to fix this issue.)");
                            }

                            if (tileErrorsAfter.Count > 0)
                            {
                                msg +=
                                    ($"\nupstream tiles or improperly parented tiles at \n{string.Join("\n", tileErrorsAfter)}\n");
                            }

                            Assert.Fail(msg);
                        }
                    }
                );
            }
        );
        await pair.CleanReturnAsync();
    }
}
