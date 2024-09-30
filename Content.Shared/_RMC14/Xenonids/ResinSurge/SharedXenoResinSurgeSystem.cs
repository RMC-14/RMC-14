using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Fruit;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.ResinSurge;

public sealed class SharedXenoResinSurgeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoConstructReinforceSystem _xenoReinforce = default!;
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
            Spawn(xeno.Comp.UnstableWallId, target);
    }

    private void ReduceSurgeCooldown(Entity<XenoResinSurgeComponent> xeno, TimeSpan? cooldown = null)
    {
        foreach (var action in _actions.GetActions(xeno))
        {
            if (TryComp(action.Id, out XenoResinSurgeActionComponent? actionComp))
            {
                _actions.SetCooldown(action.Id, cooldown ?? actionComp.FailCooldown);
                break;
            }
        }
    }


    private void OnXenoResinSurgeAction(Entity<XenoResinSurgeComponent> xeno, ref XenoResinSurgeActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Coords is not { } target)
            return;

        // Check if target on grid
        if (_transform.GetGrid(target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        target = target.SnapToGrid(EntityManager, _map);

        // Check if user has enough plasma
        if (!_xenoPlasma.TryRemovePlasmaPopup((xeno.Owner, null), args.PlasmaCost))
            return;

        if (args.Entity is { } entity)
        {
            // Check if target is xeno wall or door
            if (TryComp(entity, out XenoConstructComponent? construct))
            {
                // TODO: Check if structure is from our hive

                // Check if target is already buffed
                if (HasComp<XenoConstructReinforceComponent>(entity))
                {
                    // If yes, display popup, and start half-cooldown
                    _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-shield-fail", ("target", entity)), xeno, xeno);
                    ReduceSurgeCooldown(xeno);
                    // This is here so SharedActionsSystem doesn't start the cooldown itself
                    args.Handled = false;
                    return;
                }

                // If no, buff structure
                var popupSelf = Loc.GetString("rmc-xeno-resin-surge-shield-self", ("target", entity));
                var popupOthers = Loc.GetString("rmc-xeno-resin-surge-shield-others", ("xeno", xeno), ("target", entity));
                _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

                _xenoReinforce.Reinforce(entity, xeno.Comp.ReinforceAmount, xeno.Comp.ReinforceDuration);
                return;
            }

            // Check if target is fruit
            if (TryComp(entity, out XenoFruitComponent? fruit))
            {
                // TODO: Check if fruit is from our hive

                // Check if fruit mature, try to fasten its growth if not
                if (!_xenoFruit.TrySpeedupGrowth((entity, fruit), xeno.Comp.FruitGrowth))
                {
                    _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-fruit-fail", ("target", entity)), xeno, xeno);
                    ReduceSurgeCooldown(xeno);
                    // This is here so SharedActionsSystem doesn't start the cooldown itself
                    args.Handled = false;
                    return;
                }

                _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-fruit", ("target", entity)), xeno, xeno);
                return;
            }

            // Check if target is on weeds
            if (TryComp(entity, out XenoWeedsComponent? weeds))
            {
                // TODO: Check for hive

                var popupSelf = Loc.GetString("rmc-xeno-resin-surge-wall-self");
                var popupOthers = Loc.GetString("rmc-xeno-resin-surge-wall-others", ("xeno", xeno));
                _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

                SurgeUnstableWall(xeno, target);
                return;
            }
        }

        // TODO: implement sticky resin patch
        // Check if target is on turf
            // Start do-after of 1 second
            // If not interrupted, create a 3x3 patch of sticky resin

        // Temporary until sticky resin is added
        ReduceSurgeCooldown(xeno, TimeSpan.Zero);
        // This is here so SharedActionsSystem doesn't start the cooldown itself
        args.Handled = false;
    }
}
