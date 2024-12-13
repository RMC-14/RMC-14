using System.Diagnostics.CodeAnalysis;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Burrow;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Burrow;

public sealed partial class XenoBurrowSystem : SharedXenoBurrowSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoBurrowComponent, XenoBurrowActionEvent>(OnBeginBurrow);

        SubscribeLocalEvent<XenoBurrowComponent, XenoBurrowDownDoAfter>(OnFinishBurrow);
        SubscribeLocalEvent<XenoBurrowComponent, XenoBurrowMoveDoAfter>(OnFinishTunnel);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var time = _time.CurTime;

        var querry = EntityQueryEnumerator<XenoBurrowComponent>();
        while (querry.MoveNext(out var ent, out var comp))
        {
            if (comp.NextTunnelAt < time)
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-cooldown-finish"), ent, ent);
                comp.NextTunnelAt = null;
            }

            if (comp.ForcedUnburrowAt < time)
            {
                SetBurrow((ent, comp), false);
                comp.ForcedUnburrowAt = null;
            }
        }
    }
    private void OnBeginBurrow(EntityUid ent, XenoBurrowComponent comp, ref XenoBurrowActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        if (comp.Active)
        {
            var target = args.Target;
            if (!CanTunnelPopup((ent, comp), target, out var distance))
            {
                return;
            }
            var duration = new TimeSpan(0, 0, (int)distance);
            var moveEv = new XenoBurrowMoveDoAfter(_entities.GetNetCoordinates(target));
            var moveDoAfterArgs = new DoAfterArgs(_entities, ent, duration, moveEv, ent) { RequireCanInteract = false };
            _doAfter.TryStartDoAfter(moveDoAfterArgs);

            comp.NextTunnelAt = null;
            comp.ForcedUnburrowAt = null;
            _audio.PlayPvs(comp.BurrowDownSound, ent);
        }
        else
        {
            if (TryComp(ent, out DoAfterComponent? doAfterComp))
            {
                foreach (var doAfter in doAfterComp.DoAfters)
                {
                    if (!doAfter.Value.Cancelled && !doAfter.Value.Completed)
                    {
                        _popup.PopupEntity("We can't do that right now!", ent, ent, PopupType.SmallCaution);
                        return;
                    }
                }
            }

            if (!CanBurrowPopup((ent, comp)))
                return;

            var burrowEv = new XenoBurrowDownDoAfter();
            var burrowDoAfterArgs = new DoAfterArgs(_entities, ent, comp.BurrowLength, burrowEv, ent)
            {
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.None,
                CancelDuplicate = true
            };

            if (_doAfter.TryStartDoAfter(burrowDoAfterArgs))
                _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-down-start"), ent, ent);
        }
    }

    private void OnFinishBurrow(EntityUid ent, XenoBurrowComponent comp, ref XenoBurrowDownDoAfter args)
    {
        if (args.Handled)
        {
            return;
        }

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-down-failure-break"), ent, ent);
            return;
        }

        if (HasComp<XenoRestingComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-down-failure-rest"), ent, ent);
            return;
        }

        comp.ForcedUnburrowAt = _time.CurTime + comp.BurrowMaxDuration;
        comp.NextTunnelAt = _time.CurTime + comp.TunnelCooldown;

        SetBurrow((ent, comp), true);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-down-finish"), ent, ent);
    }

    private void OnFinishTunnel(EntityUid ent, XenoBurrowComponent comp, ref XenoBurrowMoveDoAfter args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        _transform.SetCoordinates(ent, _entities.GetCoordinates(args.TargetCoords));
        SetBurrow((ent, comp), false);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-finish"), ent, ent);
    }

    private bool CanBurrowPopup(Entity<XenoBurrowComponent> ent)
    {
        var coordinates = _transform.GetMoverCoordinates(ent.Owner).SnapToGrid();

        if (!_area.TryGetArea(coordinates, out var area, out _, out _)  ||
            area.NoTunnel)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-down-failure-bad-area"), ent, ent);
            return false;
        }

        var gridId = _transform.GetGrid(ent.Owner);

        if (TryComp(gridId, out MapGridComponent? grid))
        {
            var tile = _map.GetTileRef(gridId.Value, grid, coordinates);
            if (tile.IsSpace())
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
                return false;
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
            return false;
        }

        return true;
    }

    private bool CanTunnelPopup(Entity<XenoBurrowComponent> ent, EntityCoordinates target, [NotNullWhen(true)] out float? distance)
    {
        distance = null;

        if (ent.Comp.NextTunnelAt > _time.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-failure-coolown"), ent, ent);
            return false;
        }

        if (!_area.TryGetArea(target, out var area, out _, out _) ||
            area.NoTunnel)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-failure-bad-area"), ent, ent);
            return false;
        }

        var gridId = _transform.GetGrid(ent.Owner);

        if (TryComp(gridId, out MapGridComponent? grid))
        {
            var tile = _map.GetTileRef(gridId.Value, grid, target);
            if (tile.IsSpace())
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
                return false;
            }

            if (_turf.IsTileBlocked(tile, CollisionGroup.Impassable))
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-failure-solid"), ent, ent);
                return false;
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
            return false;
        }

        if (!target.TryDistance(_entities, ent.Owner.ToCoordinates(), out var burrowDistance))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-failure"), ent, ent);
            return false;
        }
        if (distance > ent.Comp.MaxTunnelingDistance)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-failure"), ent, ent);
            return false;
        }

        _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-start"), ent, ent);
        distance = burrowDistance;
        return true;
    }

    private void SetBurrow(Entity<XenoBurrowComponent> xenoBurrower, bool active)
    {
        var (ent, comp) = xenoBurrower;
        if (active == comp.Active)
        {
            return;
        }

        comp.Active = active;
        _actionBlocker.UpdateCanMove(ent);

        var actions = _action.GetActions(ent);

        TryComp(ent, out RMCNightVisionVisibleComponent? nightVisionComp);
        _appearance.SetData(xenoBurrower, XenoVisualLayers.Burrow, comp.Active);
        if (active)
        {
            if (nightVisionComp is not null)
            {
                nightVisionComp.Transparency = 1f;
            }

            foreach (var action in actions)
            {
                var actComp = action.Comp;

                if (actComp.BaseEvent is XenoBurrowActionEvent)
                {
                    continue;
                }
                actComp.Enabled = false;
                Dirty(action.Id, actComp);
            }
            _transform.AnchorEntity(ent);
        }
        else
        {
            if (nightVisionComp is not null)
            {
                nightVisionComp.Transparency = null;
            }

            foreach (var action in actions)
            {
                var actComp = action.Comp;

                actComp.Enabled = true;
                Dirty(action.Id, actComp);
            }

            _transform.Unanchor(ent);
            _physics.SetBodyType(ent, BodyType.KinematicController);

            _audio.PlayPvs(comp.BurrowUpSound, ent);
        }

        Dirty(xenoBurrower);
        if (nightVisionComp is RMCNightVisionVisibleComponent nightVisionCompValue)
            Dirty(ent, nightVisionCompValue);
    }
}
