using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Xenonids.Weeds;

[ToolshedCommand, AdminCommand(AdminFlags.Spawn)]
public sealed class RemoveInvalidWeedsCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Run([CommandInvocationContext] IInvocationContext ctx)
    {
        var mapSystem = GetSys<SharedMapSystem>();
        var turfSystem = GetSys<TurfSystem>();
        var blockQuery = EntityManager.GetEntityQuery<BlockWeedsComponent>();
        var weeds = EntityManager.AllEntityQueryEnumerator<XenoWeedsComponent, TransformComponent>();
        var removed = 0;
        while (weeds.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid is not { } gridId ||
                !TryComp(gridId, out MapGridComponent? grid))
            {
                continue;
            }

            var tile = mapSystem.CoordinatesToTile(gridId, grid, xform.Coordinates);
            var anchored = mapSystem.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, grid, tile);
            if (mapSystem.TryGetTileRef(gridId, grid, tile, out var tileRef) && !turfSystem.GetContentTileDefinition(tileRef).WeedsSpreadable)
            {
                EntityManager.QueueDeleteEntity(uid);
                removed++;
                continue;
            }

            while (anchored.MoveNext(out var anchoredId))
            {
                if (blockQuery.HasComp(anchoredId))
                {
                    EntityManager.QueueDeleteEntity(uid);
                    removed++;
                    break;
                }
            }
        }

        ctx.WriteLine($"Removed {removed} invalid weeds.");
    }
}
