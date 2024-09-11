using Content.Server.Administration;
using Content.Shared._RMC14.Areas;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Areas;

[ToolshedCommand, AdminCommand(AdminFlags.Host)]
public sealed class AreasCommand : ToolshedCommand
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

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

    [CommandImplementation("load")]
    public void Load()
    {
        Load(_ => true);
    }

    [CommandImplementation("loadmortar")]
    public void LoadMortar()
    {
        Load(a => a.MortarFire);
    }

    private void Load(Predicate<AreaComponent> predicate)
    {
        _map = GetSys<MapSystem>();

        var query = EntityManager.AllEntityQueryEnumerator<AreaGridComponent, MapGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var areas, out var mapGrid, out var xform))
        {
            foreach (var (position, protoId) in areas.Areas)
            {
                if (!_prototypes.TryIndex(protoId, out var proto))
                    continue;

                if (!proto.TryGetComponent(out AreaComponent? areaComp, _compFactory))
                    continue;

                if (!predicate(areaComp))
                    continue;

                var coordinates = _map.ToCoordinates(uid, position, mapGrid);
                Spawn(protoId, coordinates);
            }
        }
    }
}
