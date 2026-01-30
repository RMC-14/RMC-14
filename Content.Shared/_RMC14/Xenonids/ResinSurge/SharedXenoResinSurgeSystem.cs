using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Fruit;
using Content.Shared._RMC14.Xenonids.Fruit.Components;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Popups;
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
    [Dependency] private readonly SharedMapSystem _sharedMap = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _weeds = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoResinSurgeComponent, XenoResinSurgeActionEvent>(OnXenoResinSurgeAction);
        SubscribeLocalEvent<XenoResinSurgeComponent, ResinSurgeStickyResinDoafter>(OnResinSurgeDoAfter);
    }

    private void SurgeUnstableWall(Entity<XenoResinSurgeComponent> xeno, EntityCoordinates target)
    {
        if (!target.IsValid(EntityManager))
            return;

        if (_net.IsServer)
        {
            var wall = Spawn(xeno.Comp.UnstableWallId, target);
            _hive.SetSameHive(xeno.Owner, wall);
        }
    }

    private void SurgeStickyResin(Entity<XenoResinSurgeComponent> xeno, EntityCoordinates target)
    {
        if (!target.IsValid(EntityManager))
            return;

        if (_net.IsServer)
        {
            var resin = SpawnAtPosition(xeno.Comp.StickyResinId, target);
            _hive.SetSameHive(xeno.Owner, resin);
        }
    }


    private void ReduceSurgeCooldown(Entity<XenoResinSurgeComponent> xeno, double? cooldownMult = null)
    {
        foreach (var action in _actions.GetActions(xeno))
        {
            if (TryComp(action, out XenoResinSurgeActionComponent? actionComp))
            {
                _actions.SetCooldown(action.AsNullable(), actionComp.SuccessCooldown * (cooldownMult ?? actionComp.FailCooldownMult));
                break;
            }
        }
    }

    private void SetSurgeCooldown(Entity<XenoResinSurgeComponent> xeno, TimeSpan? cooldown = null)
    {
        foreach (var action in _actions.GetActions(xeno))
        {
            if (TryComp(action, out XenoResinSurgeActionComponent? actionComp))
            {
                _actions.SetCooldown(action.AsNullable(), cooldown ?? actionComp.SuccessCooldown);
                break;
            }
        }
    }

    private void OnXenoResinSurgeAction(Entity<XenoResinSurgeComponent> xeno, ref XenoResinSurgeActionEvent args)
    {
        if (args.Handled)
            return;

        // Check if target on grid
        if (_transform.GetGrid(args.Target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        if (!_examine.InRangeUnOccluded(xeno.Owner, args.Target, xeno.Comp.Range))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-resin-surge-see-fail"), xeno, xeno);
            return;
        }

        args.Handled = true;

        var target = args.Target.SnapToGrid(EntityManager, _map);

        // Check if user has enough plasma
        if (xeno.Comp.ResinDoafter != null || !_xenoPlasma.TryRemovePlasmaPopup((xeno.Owner, null), args.PlasmaCost))
            return;

        if (args.Entity is { } entity)
        {
            // Check if target is xeno wall or door
            if (TryComp(entity, out ResinSurgeReinforcableComponent? construct) && _hive.FromSameHive(xeno.Owner, entity))
            {
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
                if (_net.IsServer)
                {
                    if (HasComp<DoorComponent>(entity))
                        SpawnAttachedTo(xeno.Comp.SurgeDoorEffect, entity.ToCoordinates());
                    else
                        SpawnAttachedTo(xeno.Comp.SurgeWallEffect, entity.ToCoordinates());
                }
                return;
            }

            // Check if target is fruit
            if (TryComp(entity, out XenoFruitComponent? fruit) && _hive.FromSameHive(xeno.Owner, entity))
            {
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
                args.Handled = false;
                var cooldownTimeMult = (fruit.GrowTime.TotalSeconds - (fruit.GrowTime / xeno.Comp.FruitCooldownDivisor)) * 0.1;
                ReduceSurgeCooldown(xeno, cooldownTimeMult);

                return;
            }

            // Check if target is on weeds
            if (TryComp(entity, out XenoWeedsComponent? weeds) || _weeds.IsOnFriendlyWeeds(entity))
            {
                EntityUid weedEnt = entity;
                if (weeds == null)
                {
                    var weedTempEnt = _weeds.GetWeedsOnFloor((gridId, grid), entity.ToCoordinates());
                    if (weedTempEnt != null)
                    {
                        weedEnt = weedTempEnt.Value;
                        TryComp(weedEnt, out weeds);
                    }
                }

                if (weeds != null && _hive.FromSameHive(xeno.Owner, weedEnt))
                {
                    var popupSelf = Loc.GetString("rmc-xeno-resin-surge-wall-self");
                    var popupOthers = Loc.GetString("rmc-xeno-resin-surge-wall-others", ("xeno", xeno));
                    _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

                    SurgeUnstableWall(xeno, target);
                    return;
                }
            }
        }

        // Sticky Resin
        var ev = new ResinSurgeStickyResinDoafter(GetNetCoordinates(target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.StickyResinDoAfterPeriod, ev, xeno) { BreakOnMove = true, DuplicateCondition = DuplicateConditions.SameEvent };
        if (_doAfter.TryStartDoAfter(doAfter, out var id))
            xeno.Comp.ResinDoafter = id;
        else
            ReduceSurgeCooldown(xeno);

        args.Handled = false;
    }

    private void OnResinSurgeDoAfter(Entity<XenoResinSurgeComponent> xeno, ref ResinSurgeStickyResinDoafter args)
    {
        xeno.Comp.ResinDoafter = null;
        if (args.Cancelled)
            return;
        var coords = GetCoordinates(args.Coordinates);
        if (_transform.GetGrid(coords) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        var popupSelf = Loc.GetString("rmc-xeno-resin-surge-sticky-self");
        var popupOthers = Loc.GetString("rmc-xeno-resin-surge-sticky-others", ("xeno", xeno));
        _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

        if (_net.IsServer)
        {
            foreach (var turf in _sharedMap.GetTilesIntersecting(gridId, grid, Box2.CenteredAround(coords.Position, new(xeno.Comp.StickyResinRadius * 2, xeno.Comp.StickyResinRadius * 2)), false))
            {
                if (!_rmcMap.HasAnchoredEntityEnumerator<StickyResinSurgeBlockerComponent>(_turf.GetTileCenter(turf), out _))
                    SurgeStickyResin(xeno, _turf.GetTileCenter(turf));
            }
        }

        SetSurgeCooldown(xeno);
    }
}
