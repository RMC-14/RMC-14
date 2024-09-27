using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Fruit;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._RMC14.Xenonids.ResinSurge;

public sealed class SharedXenoResinSurgeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoFruitSystem _xenoFruit = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;


    private EntityQuery<XenoConstructComponent> _xenoConstructQuery;
    private EntityQuery<XenoFruitComponent> _xenoFruitQuery;
    private EntityQuery<XenoWeedsComponent> _xenoWeedsQuery;


    public override void Initialize()
    {
        _xenoConstructQuery = GetEntityQuery<XenoConstructComponent>();
        _xenoFruitQuery = GetEntityQuery<XenoFruitComponent>();
        _xenoWeedsQuery = GetEntityQuery<XenoWeedsComponent>();

        SubscribeLocalEvent<XenoResinSurgeComponent, XenoResinSurgeActionEvent>(OnXenoResinSurgeAction);
    }


    private void OnXenoResinSurgeAction(Entity<XenoResinSurgeComponent> xeno, ref XenoResinSurgeActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var target = args.Target;

        // Check if target on grid
        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        // Check if user has enough plasma
        if (!_xenoPlasma.HasPlasmaPopup((xeno.Owner, null), xeno.Comp.PlasmaCost))
            return;

        target = target.SnapToGrid(EntityManager, _map);
        var tile = _mapSystem.CoordinatesToTile(gridId, grid, target);
        var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);

        while (anchored.MoveNext(out var uid))
        {
             // Check if target is xeno wall or door
            if (_xenoConstructQuery.TryComp(uid, out XenoConstructComponent? construct))
            {
                // TODO: Check if structure is from our hive
                // Check if target is already buffed
                // If yes, display popup, and start half-cooldown
                // If no, buff structure
                break;
            }

            // Check if target is fruit
            if (_xenoFruitQuery.TryComp(uid, out XenoFruitComponent? fruit))
            {
                // TODO: Check if fruit is from our hive

                // Check if fruit is already mature
                if (fruit.State != XenoFruitState.Growing)
                {
                    // If yes, display, popup, start half-cooldown
                    _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-fruit-fail", ("target", uid)), xeno, xeno);
                    // TODO: reduce cooldown by half
                }
                else
                {
                    // If no, speed up fruit growth
                    _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-fruit-fail", ("target", uid)), xeno, xeno);
                    _xenoFruit.TrySpeedupGrowth(fruit, xeno.Comp.FruitGrowth);
                }

                break;
            }

            // Check if target is on weeds
            if (_xenoWeedsQuery.TryComp(uid, out XenoWeedsComponent? weeds))
            {
                // TODO: Check for hive
                // Check for other obstructions
                // If no obstructions, create weak temporary wall
                // The temporary wall should collapse within 5 seconds
                break;
            }


        }

       // Check if target is on turf
            // Start do-after of 1 second
            // If not interrupted, create a 3x3 patch of sticky resin
    }

}
