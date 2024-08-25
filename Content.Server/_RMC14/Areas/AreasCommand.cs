using Content.Server.Administration;
using Content.Shared._RMC14.Areas;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Areas;

[ToolshedCommand, AdminCommand(AdminFlags.Host)]
public sealed class AreasCommand : ToolshedCommand
{
    private MapSystem? _map;

    [CommandImplementation("save")]
    public void Save([CommandInvocationContext] IInvocationContext ctx)
    {
        _map = GetSys<MapSystem>();

        var gridQuery = GetEntityQuery<MapGridComponent>();

        var query = EntityManager.AllEntityQueryEnumerator<AreaComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var metaData, out var xform))
        {
            if (xform.GridUid is not { } gridId ||
                !gridQuery.TryComp(gridId, out var grid))
            {
                continue;
            }

            var areaGrid = EnsureComp<AreaGridComponent>(gridId);
            if (metaData.EntityPrototype is not { } prototype)
            {
                ctx.WriteLine($"{EntityManager.ToPrettyString(uid)} did not have a prototype.");
                continue;
            }

            var indices = _map.TileIndicesFor(gridId, grid, xform.Coordinates);
            var areas = areaGrid.Areas;
            areas[indices] = prototype.ID;
            QDel(uid);
        }
    }
}
