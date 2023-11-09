using System.Numerics;
using Content.Server.Spreader;
using Content.Shared.CM14.Xenos.Construction;
using Robust.Server.GameObjects;

namespace Content.Server.CM14.Xenos.Construction;

public sealed class XenoConstructionSystem : SharedXenoConstructionSystem
{
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoWeedsComponent, SpreadNeighborsEvent>(OnWeedsSpreadNeighbors);
    }

    private void OnWeedsSpreadNeighbors(Entity<XenoWeedsComponent> ent, ref SpreadNeighborsEvent args)
    {
        var source = ent.Comp.IsSource ? ent.Owner : ent.Comp.Source;

        // TODO CM14 wall texture
        if (args.NeighborFreeTiles.Count <= 0 ||
            !Exists(source) ||
            !TryComp(source, out TransformComponent? transform) ||
            ent.Comp.Spawns.Id is not { } prototype)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        var any = false;
        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var gridOwner = neighbor.Grid.Owner;
            var coords = _mapSystem.GridTileToLocal(gridOwner, neighbor.Grid, neighbor.Tile);

            var sourceLocal = _mapSystem.CoordinatesToTile(gridOwner, neighbor.Grid, transform.Coordinates);
            var diff = Vector2.Abs(neighbor.Tile - sourceLocal);
            if (diff.X >= ent.Comp.Range || diff.Y >= ent.Comp.Range)
                break;

            var neighborWeeds = Spawn(prototype, coords);
            var neighborWeedsComp = EnsureComp<XenoWeedsComponent>(neighborWeeds);

            neighborWeedsComp.IsSource = false;
            neighborWeedsComp.Source = source;

            EnsureComp<ActiveEdgeSpreaderComponent>(neighborWeeds);

            any = true;
        }

        if (!any)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
        }

        args.Updates--;
    }
}
