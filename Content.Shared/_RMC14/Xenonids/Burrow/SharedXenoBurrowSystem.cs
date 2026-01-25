using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Burrow;

/// <summary>
/// Deals with Burrowing, where a xeno goes into the ground and shortly comes back up
/// </summary>
public abstract class SharedXenoBurrowSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBurrowComponent, ExamineAttemptEvent>(PreventExamine);

        SubscribeLocalEvent<XenoBurrowComponent, BeforeStatusEffectAddedEvent>(PreventEffects);
        SubscribeLocalEvent<XenoBurrowComponent, BeforeDamageChangedEvent>(PreventDamage);
        SubscribeLocalEvent<XenoBurrowComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<XenoBurrowComponent, InteractionAttemptEvent>(PreventInteraction);
        SubscribeLocalEvent<XenoBurrowComponent, RMCIgniteAttemptEvent>(OnBurrowedCancel);
        SubscribeLocalEvent<XenoBurrowComponent, AttackAttemptEvent>(OnBurrowedCancel);

        SubscribeLocalEvent<XenoBurrowComponent, XenoBurrowActionEvent>(OnBeginBurrow);
        SubscribeLocalEvent<XenoBurrowComponent, XenoBurrowDownDoAfter>(OnFinishBurrow);
        SubscribeLocalEvent<XenoBurrowComponent, BurrowedEvent>(SetBurrow);
        SubscribeLocalEvent<XenoBurrowComponent, XenoBurrowMoveDoAfter>(OnFinishTunnel);

    }

    private void PreventExamine(Entity<XenoBurrowComponent> burrower, ref ExamineAttemptEvent args)
    {
        if (args.Cancelled || !burrower.Comp.Active)
            return;

        if (HasComp<XenoComponent>(args.Examiner))
            return;

        args.Cancel();
    }

    private void PreventEffects(Entity<XenoBurrowComponent> burrower, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Cancelled || !burrower.Comp.Active)
            return;

        // Note: If any beneficial effects is added that makes sense underground, this may have to be more precise
        args.Cancelled = true;
    }

    private void OnBurrowedCancel<T>(Entity<XenoBurrowComponent> burrower, ref T args) where T : CancellableEntityEventArgs
    {
        if (args.Cancelled || !burrower.Comp.Active)
            return;

        args.Cancel();
    }

    private void PreventDamage(Entity<XenoBurrowComponent> burrower, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || !burrower.Comp.Active)
            return;

        args.Cancelled = true;
    }

    private void PreventCollision(Entity<XenoBurrowComponent> burrower, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !burrower.Comp.Active)
            return;

        args.Cancelled = true;
    }

    private void PreventInteraction(EntityUid ent, XenoBurrowComponent comp, ref InteractionAttemptEvent args)
    {
        if (args.Cancelled || !comp.Active)
        {
            return;
        }

        args.Cancelled = true;
    }

    private void SetBurrow(Entity<XenoBurrowComponent> burrower, ref BurrowedEvent args)
    {
        if (args.burrowed == burrower.Comp.Active)
            return;

        burrower.Comp.Active = args.burrowed;
        _actionBlocker.UpdateCanMove(burrower);

        if (args.burrowed)
        {
            _transform.AnchorEntity(burrower);
        }
        else
        {
            _transform.Unanchor(burrower);
            if (TryComp(burrower, out PhysicsComponent? body))
            {
                _physics.TrySetBodyType(burrower, BodyType.KinematicController, body: body);
                Dirty(burrower, body);
            }

            if (_net.IsServer)
            {
                _audio.PlayPvs(burrower.Comp.BurrowUpSound, burrower);

                var entitiesToStun = _entityLookup.GetEntitiesInRange(burrower, burrower.Comp.UnburrowStunRange);
                foreach (var entity in entitiesToStun)
                {
                    if (!_xeno.CanAbilityAttackTarget(burrower, entity))
                        continue;

                    _stun.TryParalyze(entity, burrower.Comp.UnburrowStunLength, false);
                }
            }

            Dirty(burrower);
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _time.CurTime;

        var querry = EntityQueryEnumerator<XenoBurrowComponent>();
        while (querry.MoveNext(out var ent, out var comp))
        {
            if (comp.NextBurrowAt < time)
            {
                if (comp.Active)
                {
                    //Commented out since it doesn't really mean anything while burrowed
                    //_popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-resurface-cooldown-finish"), ent, ent);
                }
                else
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-cooldown-finish"), ent, ent);
                comp.NextBurrowAt = null;
            }

            if (comp.NextTunnelAt < time)
            {
                _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-cooldown-finish"), ent, ent);
                comp.NextTunnelAt = null;
            }

            if (!comp.Tunneling && comp.ForcedUnburrowAt < time)
            {
                var ev = new BurrowedEvent(false);
                RaiseLocalEvent(ent, ref ev);
                comp.ForcedUnburrowAt = null;
                _popup.PopupEntity(Loc.GetString("rmc-xeno-burrow-move-forced-unburrow"), ent, ent, PopupType.MediumCaution);
                comp.NextBurrowAt = time + comp.BurrowCooldown;
            }
        }
    }

    private void OnBeginBurrow(Entity<XenoBurrowComponent> burrower, ref XenoBurrowActionEvent args)
    {
        if (args.Handled)
            return;

        if (burrower.Comp.Active)
        {
            var target = args.Target;
            if (!CanTunnelPopup(burrower, target, out var distance))
                return;
            var duration = new TimeSpan(0, 0, (int)distance);
            if (duration < burrower.Comp.MinimumTunnelTime)
                duration = burrower.Comp.MinimumTunnelTime;
            var moveEv = new XenoBurrowMoveDoAfter(_entities.GetNetCoordinates(target));
            var moveDoAfterArgs = new DoAfterArgs(_entities, burrower, duration, moveEv, burrower) { RequireCanInteract = false, DuplicateCondition = DuplicateConditions.SameEvent };

            if (_doAfter.TryStartDoAfter(moveDoAfterArgs))
            {
                burrower.Comp.Tunneling = true;
                Dirty(burrower);
            }

            Dirty(burrower);
            if (_net.IsServer)
                _audio.PlayPvs(burrower.Comp.BurrowDownSound, burrower);
        }
        else
        {

            if (TryComp(burrower, out DoAfterComponent? doAfterComp))
            {
                foreach (var doAfter in doAfterComp.DoAfters)
                {
                    if (!doAfter.Value.Cancelled && !doAfter.Value.Completed)
                    {
                        _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-down-doafter-stop"), burrower, burrower, PopupType.SmallCaution);
                        return;
                    }
                }
            }

            if (!CanBurrowPopup(burrower))
                return;

            var burrowEv = new XenoBurrowDownDoAfter();
            var burrowDoAfterArgs = new DoAfterArgs(_entities, burrower, burrower.Comp.BurrowLength, burrowEv, burrower)
            {
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameEvent,
                CancelDuplicate = true
            };

            if (_doAfter.TryStartDoAfter(burrowDoAfterArgs))
                _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-down-start"), burrower, burrower);
        }
    }

    private void OnFinishBurrow(Entity<XenoBurrowComponent> burrower, ref XenoBurrowDownDoAfter args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-down-failure-break"), burrower, burrower);
            burrower.Comp.NextBurrowAt = _time.CurTime + burrower.Comp.BurrowCooldown;
            return;
        }

        if (HasComp<XenoRestingComponent>(burrower))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-down-failure-rest"), burrower, burrower);
            return;
        }

        burrower.Comp.ForcedUnburrowAt = _time.CurTime + burrower.Comp.BurrowMaxDuration;
        burrower.Comp.NextBurrowAt = _time.CurTime + burrower.Comp.BurrowCooldown;
        _rmcPulling.TryStopAllPullsFromAndOn(burrower);
        Dirty(burrower);

        var ev = new BurrowedEvent(true);
        RaiseLocalEvent(burrower, ref ev);
        _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-down-finish"), burrower, burrower);
    }

    private bool CanBurrowPopup(Entity<XenoBurrowComponent> ent)
    {
        if (ent.Comp.NextBurrowAt > _time.CurTime)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-down-failure-cooldown"), ent, ent);
            return false;
        }

        var coordinates = _transform.GetMoverCoordinates(ent.Owner).SnapToGrid();

        if (!_area.TryGetArea(coordinates, out var area, out _) ||
            area.Value.Comp.NoTunnel)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-down-failure-bad-area"), ent, ent);
            return false;
        }

        var gridId = _transform.GetGrid(ent.Owner);

        if (TryComp(gridId, out MapGridComponent? grid))
        {
            var tile = _map.GetTileRef(gridId.Value, grid, coordinates);
            if (_turf.IsSpace(tile))
            {
                _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
                return false;
            }
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
            return false;
        }

        return true;
    }

    private bool CanTunnelPopup(Entity<XenoBurrowComponent> ent, EntityCoordinates target, [NotNullWhen(true)] out float? distance)
    {
        distance = null;

        if (ent.Comp.NextTunnelAt > _time.CurTime)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-failure-coolown"), ent, ent);
            return false;
        }

        if (!_area.TryGetArea(target, out var area, out _) ||
            area.Value.Comp.NoTunnel)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-failure-bad-area"), ent, ent);
            return false;
        }

        var gridId = _transform.GetGrid(ent.Owner);

        if (TryComp(gridId, out MapGridComponent? grid))
        {
            var tile = _map.GetTileRef(gridId.Value, grid, target);
            if (_turf.IsSpace(tile))
            {
                _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
                return false;
            }

            if (_turf.IsTileBlocked(tile, CollisionGroup.Impassable))
            {
                _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-failure-solid"), ent, ent);
                return false;
            }
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-failure-space"), ent, ent);
            return false;
        }

        if (!target.TryDistance(_entities, ent.Owner.ToCoordinates(), out var burrowDistance))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-failure"), ent, ent);
            return false;
        }
        if (distance > ent.Comp.MaxTunnelingDistance)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-failure"), ent, ent);
            return false;
        }

        if (!ent.Comp.Tunneling)
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-start"), ent, ent);
        else
            _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-break"), ent, ent);
        distance = burrowDistance;
        return true;
    }

    private void OnFinishTunnel(Entity<XenoBurrowComponent> burrower, ref XenoBurrowMoveDoAfter args)
    {
        if (args.Handled || args.Cancelled)
        {
            burrower.Comp.Tunneling = false;
            burrower.Comp.NextTunnelAt = _time.CurTime + burrower.Comp.TunnelCooldown;
            return;
        }

        burrower.Comp.Tunneling = false;
        burrower.Comp.NextTunnelAt = null;
        burrower.Comp.ForcedUnburrowAt = null;
        _rmcPulling.TryStopAllPullsFromAndOn(burrower);
        Dirty(burrower);
        if (_net.IsServer)
            _transform.SetCoordinates(burrower, _entities.GetCoordinates(args.TargetCoords));
        var ev = new BurrowedEvent(false);
        RaiseLocalEvent(burrower, ref ev);
        _popup.PopupClient(Loc.GetString("rmc-xeno-burrow-move-finish"), burrower, burrower);
    }
}

public sealed partial class XenoBurrowActionEvent : WorldTargetActionEvent;

/// <summary>
/// Called when a Xeno starts to burrow towards a specific tile
/// </summary>
[Serializable, NetSerializable]
public sealed partial class XenoBurrowMoveDoAfter : SimpleDoAfterEvent
{
    public NetCoordinates TargetCoords;
    public XenoBurrowMoveDoAfter(NetCoordinates targetCoords)
    {
        TargetCoords = targetCoords;
    }
}
/// <summary>
/// Called when a xeno starts to burrow down into the current tile
/// </summary>
[Serializable, NetSerializable]
public sealed partial class XenoBurrowDownDoAfter : SimpleDoAfterEvent;
