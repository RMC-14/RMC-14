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
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.ResinSurge;

public sealed class SharedXenoResinSurgeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruct = default!;
    [Dependency] private readonly SharedXenoFruitSystem _xenoFruit = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoResinSurgeComponent, XenoResinSurgeActionEvent>(OnXenoResinSurgeAction);
    }

    private void SurgeUnstableWall(Entity<XenoResinSurgeComponent> xeno, EntityCoordinates target)
    {
        if (!target.IsValid(EntityManager))
            return;

        if (_net.IsServer)
        {
            var structure = Spawn(xeno.Comp.UnstableWallId, target);
        }
    }


    private void OnXenoResinSurgeAction(Entity<XenoResinSurgeComponent> xeno, ref XenoResinSurgeActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Coords is not { } target)
            return;

        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        target = target.SnapToGrid(EntityManager, _map);
        //var tile = _mapSystem.CoordinatesToTile(gridId, grid, target);
        //var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);

        // Check if user has enough plasma
        if (!_xenoPlasma.HasPlasmaPopup((xeno.Owner, null), xeno.Comp.PlasmaCost))
            return;

        if (args.Entity is { } entity)
        {
            // Check if target is xeno wall or door
            if (TryComp(entity, out XenoConstructComponent? construct))
            {
                // TODO: Check if structure is from our hive
                // Check if target is already buffed
                // If yes, display popup, and start half-cooldown
                // If no, buff structure
                return;
            }

            // Check if target is fruit
            if (TryComp(entity, out XenoFruitComponent? fruit))
            {
                // TODO: Check if fruit is from our hive

                if (_xenoFruit.TrySpeedupGrowth((entity, fruit), xeno.Comp.FruitGrowth))
                {
                    // If no, speed up fruit growth
                    _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-fruit", ("target", entity)), xeno, xeno);
                    return;
                }

                _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-fruit-fail", ("target", entity)), xeno, xeno);

                // TODO: reduce cooldown by half

                return;
            }

            // Check if target is on weeds
            if (TryComp(entity, out XenoWeedsComponent? weeds))
            {
                // TODO: Check for hive
                // Check for other obstructions
                // If no obstructions, create weak temporary wall
                // The temporary wall should collapse within 5 seconds

                var popupSelf = Loc.GetString("rmc-xeno-resin-surge-wall-self");
                var popupOthers = Loc.GetString("rmc-xeno-resin-surge-wall-others", ("xeno", xeno));
                _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

                SurgeUnstableWall(xeno, target);
                return;
            }
        }



        // Check if target is on turf
            // Start do-after of 1 second
            // If not interrupted, create a 3x3 patch of sticky resin

        // Check if target on grid


    }

}
