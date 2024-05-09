using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Xenos.Construction;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Egg;

public sealed class XenoEggSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, XenoGrowOvipositorActionEvent>(OnXenoGrowOvipositorAction);
        SubscribeLocalEvent<XenoComponent, XenoGrowOvipositorDoAfterEvent>(OnXenoGrowOvipositorDoAfter);

        SubscribeLocalEvent<XenoAttachedOvipositorComponent, MapInitEvent>(OnXenoAttachedMapInit);
        SubscribeLocalEvent<XenoAttachedOvipositorComponent, ComponentRemove>(OnXenoAttachedRemove);

        SubscribeLocalEvent<XenoEggComponent, AfterAutoHandleStateEvent>(OnXenoEggAfterState);
        SubscribeLocalEvent<XenoEggComponent, GettingPickedUpAttemptEvent>(OnXenoEggPickedUpAttempt);
        SubscribeLocalEvent<XenoEggComponent, AfterInteractEvent>(OnXenoEggAfterInteract);
        SubscribeLocalEvent<XenoEggComponent, ActivateInWorldEvent>(OnXenoEggActivateInWorld);
    }

    private void OnXenoGrowOvipositorAction(Entity<XenoComponent> ent, ref XenoGrowOvipositorActionEvent args)
    {
        if (args.Handled)
            return;

        var hasOvipositor = HasComp<XenoAttachedOvipositorComponent>(ent);
        if (!hasOvipositor &&
            !_plasma.HasPlasmaPopup(ent.Owner, args.AttachPlasmaCost))
        {
            return;
        }

        args.Handled = true;

        var ev = new XenoGrowOvipositorDoAfterEvent { PlasmaCost = args.AttachPlasmaCost };
        var delay = args.AttachDoAfter;
        if (hasOvipositor)
        {
            ev.PlasmaCost = FixedPoint2.Zero;
            delay = args.DetachDoAfter;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, delay, ev, ent)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnXenoGrowOvipositorDoAfter(Entity<XenoComponent> ent, ref XenoGrowOvipositorDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            !_plasma.TryRemovePlasmaPopup(ent.Owner, args.PlasmaCost))
        {
            return;
        }

        args.Handled = true;

        var attaching = !EnsureComp(ent, out XenoAttachedOvipositorComponent _);
        if (!attaching)
            RemComp<XenoAttachedOvipositorComponent>(ent);

        foreach (var (actionId, _) in _actions.GetActions(ent))
        {
            if (TryComp(actionId, out XenoGrowOvipositorActionComponent? action))
            {
                _actions.SetCooldown(actionId, attaching ? action.AttachCooldown : action.DetachCooldown);
                _actions.SetToggled(actionId, attaching);
            }
        }
    }

    private void OnXenoAttachedMapInit(Entity<XenoAttachedOvipositorComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out TransformComponent? xform))
            _transform.AnchorEntity(ent, xform);
    }

    private void OnXenoAttachedRemove(Entity<XenoAttachedOvipositorComponent> ent, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(ent) && TryComp(ent, out TransformComponent? xform))
            _transform.Unanchor(ent, xform);
    }

    private void OnXenoEggAfterState(Entity<XenoEggComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var ev = new XenoEggStateChangedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnXenoEggPickedUpAttempt(Entity<XenoEggComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.State != XenoEggState.Item)
            args.Cancel();
    }

    private void OnXenoEggAfterInteract(Entity<XenoEggComponent> ent, ref AfterInteractEvent args)
    {
        if (ent.Comp.State != XenoEggState.Item ||
            !TryComp(ent, out TransformComponent? xform))
        {
            return;
        }

        var user = args.User;
        if (!args.CanReach)
        {
            if (_timing.IsFirstTimePredicted)
                _popup.PopupCoordinates("You can't reach there!", args.ClickLocation, Filter.Local(), true);

            return;
        }

        if (HasComp<MarineComponent>(user))
        {
            // TODO CM14 this should have a better filter than marine component
            if (!args.Handled)
            {
                _hands.TryDrop(user, ent, args.ClickLocation);
                _popup.PopupClient("Best not to plant this thing outside of a containment cell.", user, user);
            }

            args.Handled = true;
            return;
        }

        if (args.ClickLocation.GetGridUid(EntityManager) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return;
        }

        var tile = _map.TileIndicesFor(gridId, grid, args.ClickLocation);
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridId, grid, tile);
        var hasWeeds = false;
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<XenoEggComponent>(uid))
            {
                _popup.PopupClient("There's already an egg there.", uid.Value, user);
                args.Handled = true;
                return;
            }

            if (HasComp<XenoWeedsComponent>(uid))
                hasWeeds = true;
        }

        // TODO CM14 only on hive weeds
        if (!hasWeeds)
        {
            _popup.PopupClient("The egg must be planted on weeds.", user, user);
            return;
        }

        // Hand code is god-awful and its reach distance is inconsistent with args.CanReach
        // so we need to set the position ourselves.
        _transform.SetCoordinates(ent, args.ClickLocation);
        _transform.SetLocalRotation(ent, 0);

        SetEggState(ent, XenoEggState.Growing);
        _transform.AnchorEntity(ent, xform);
    }

    private void OnXenoEggActivateInWorld(Entity<XenoEggComponent> ent, ref ActivateInWorldEvent args)
    {
        // TODO CM14 multiple hive support
        if (!HasComp<XenoComponent>(args.User))
            return;

        if (ent.Comp.State == XenoEggState.Opened)
        {
            args.Handled = true;
            _popup.PopupClient("We clear the hatched egg.", ent, args.User);

            if (_net.IsClient)
                return;

            QueueDel(ent);

            return;
        }

        if (ent.Comp.State != XenoEggState.Grown)
        {
            _popup.PopupClient("The egg is not developed yet.", ent, args.User);
            return;
        }

        args.Handled = true;
        SetEggState(ent, XenoEggState.Opened);

        if (_net.IsClient)
            return;

        if (TryComp(ent, out TransformComponent? xform))
            SpawnAtPosition(ent.Comp.Spawn, xform.Coordinates);
    }

    private void SetEggState(Entity<XenoEggComponent> egg, XenoEggState state)
    {
        egg.Comp.State = state;
        Dirty(egg);

        if (state == XenoEggState.Opened)
            RemCompDeferred<XenoFriendlyComponent>(egg);

        var ev = new XenoEggStateChangedEvent();
        RaiseLocalEvent(egg, ref ev);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var oviQuery = EntityQueryEnumerator<XenoOvipositorCapableComponent, XenoAttachedOvipositorComponent, TransformComponent>();
        while (oviQuery.MoveNext(out var uid, out var capable, out var attached, out var xform))
        {
            if (attached.NextEgg == null)
            {
                attached.NextEgg = time + capable.Cooldown;
                continue;
            }

            if (time < attached.NextEgg)
                continue;

            if (TryComp(uid, out MobStateComponent? state) &&
                _mobState.IsIncapacitated(uid, state))
            {
                continue;
            }

            attached.NextEgg = time + capable.Cooldown;
            Dirty(uid, attached);

            var egg = SpawnAtPosition(capable.Spawn, xform.Coordinates.Offset(capable.Offset));
            _transform.SetLocalRotation(egg, Angle.Zero);
        }

        var eggQuery = EntityQueryEnumerator<XenoEggComponent, TransformComponent>();
        while (eggQuery.MoveNext(out var uid, out var egg, out var xform))
        {
            if (!xform.Anchored ||
                egg.State != XenoEggState.Growing)
            {
                continue;
            }

            egg.GrowAt ??= time + _random.Next(egg.MinTime, egg.MaxTime);

            if (time < egg.GrowAt || egg.State != XenoEggState.Growing)
                continue;

            SetEggState((uid, egg), XenoEggState.Grown);
        }
    }
}
