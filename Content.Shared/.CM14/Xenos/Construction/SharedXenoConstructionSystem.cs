using Content.Shared.Coordinates.Helpers;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;

namespace Content.Shared.CM14.Xenos.Construction;

public abstract class SharedXenoConstructionSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, XenoPlantWeedsEvent>(OnXenoPlantWeeds);
        SubscribeLocalEvent<XenoWeedsComponent, AnchorStateChangedEvent>(OnWeedsAnchorChanged);
    }

    private void OnXenoPlantWeeds(Entity<XenoComponent> ent, ref XenoPlantWeedsEvent args)
    {
        var coordinates = _transform.GetMoverCoordinates(ent).SnapToGrid(EntityManager, _map);

        if (coordinates.GetGridUid(EntityManager) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return;
        }

        var position = _mapSystem.LocalToTile(gridUid, grid, coordinates);
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, position);

        while (enumerator.MoveNext(out var anchored))
        {
            if (HasComp<XenoWeedsComponent>(anchored))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-weeds-already-here"), ent.Owner, ent.Owner);
                return;
            }
        }

        if (!_xeno.TryRemovePlasmaPopup(ent, args.PlasmaCost))
            return;

        if (_net.IsServer)
            Spawn(args.Prototype, coordinates);
    }

    private void OnWeedsAnchorChanged(Entity<XenoWeedsComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            QueueDel(ent);
    }
}
