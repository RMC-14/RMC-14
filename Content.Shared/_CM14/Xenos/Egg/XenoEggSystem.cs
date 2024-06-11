using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Xenos.Hugger;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared._CM14.Xenos.Weeds;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
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
    [Dependency] private readonly SharedXenoHuggerSystem _hugger = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

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
        SubscribeLocalEvent<XenoEggComponent, StepTriggerAttemptEvent>(OnXenoEggStepTriggerAttempt);
        SubscribeLocalEvent<XenoEggComponent, StepTriggeredOffEvent>(OnXenoEggStepTriggered);
    }

    private void OnXenoGrowOvipositorAction(Entity<XenoComponent> xeno, ref XenoGrowOvipositorActionEvent args)
    {
        if (args.Handled)
            return;

        var hasOvipositor = HasComp<XenoAttachedOvipositorComponent>(xeno);
        if (!hasOvipositor &&
            !_plasma.HasPlasmaPopup(xeno.Owner, args.AttachPlasmaCost))
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

        var doAfterArgs = new DoAfterArgs(EntityManager, xeno, delay, ev, xeno)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnXenoGrowOvipositorDoAfter(Entity<XenoComponent> xeno, ref XenoGrowOvipositorDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            !_plasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
        {
            return;
        }

        args.Handled = true;

        var attaching = !EnsureComp(xeno, out XenoAttachedOvipositorComponent _);
        if (!attaching)
            RemComp<XenoAttachedOvipositorComponent>(xeno);

        foreach (var (actionId, _) in _actions.GetActions(xeno))
        {
            if (TryComp(actionId, out XenoGrowOvipositorActionComponent? action))
            {
                _actions.SetCooldown(actionId, attaching ? action.AttachCooldown : action.DetachCooldown);
                _actions.SetToggled(actionId, attaching);
            }
        }
    }

    private void OnXenoAttachedMapInit(Entity<XenoAttachedOvipositorComponent> attached, ref MapInitEvent args)
    {
        if (TryComp(attached, out TransformComponent? xform))
            _transform.AnchorEntity(attached, xform);
    }

    private void OnXenoAttachedRemove(Entity<XenoAttachedOvipositorComponent> attached, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(attached) && TryComp(attached, out TransformComponent? xform))
            _transform.Unanchor(attached, xform);
    }

    private void OnXenoEggAfterState(Entity<XenoEggComponent> egg, ref AfterAutoHandleStateEvent args)
    {
        var ev = new XenoEggStateChangedEvent();
        RaiseLocalEvent(egg, ref ev);
    }

    private void OnXenoEggPickedUpAttempt(Entity<XenoEggComponent> egg, ref GettingPickedUpAttemptEvent args)
    {
        if (egg.Comp.State != XenoEggState.Item)
            args.Cancel();
    }

    private void OnXenoEggAfterInteract(Entity<XenoEggComponent> egg, ref AfterInteractEvent args)
    {
        if (egg.Comp.State != XenoEggState.Item ||
            !TryComp(egg, out TransformComponent? xform))
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
                _hands.TryDrop(user, egg, args.ClickLocation);
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-failed-plant-outside"), user, user);
            }

            args.Handled = true;
            return;
        }

        if (_transform.GetGrid(args.ClickLocation) is not { } gridId ||
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
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-failed-already-there"), uid.Value, user);
                args.Handled = true;
                return;
            }

            if (HasComp<XenoWeedsComponent>(uid))
                hasWeeds = true;
        }

        // TODO CM14 only on hive weeds
        if (!hasWeeds)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-egg-failed-must-weeds"), user, user);
            return;
        }

        // Hand code is god-awful and its reach distance is inconsistent with args.CanReach
        // so we need to set the position ourselves.
        _transform.SetCoordinates(egg, args.ClickLocation);
        _transform.SetLocalRotation(egg, 0);

        SetEggState(egg, XenoEggState.Growing);
        _transform.AnchorEntity(egg, xform);
    }

    private void OnXenoEggActivateInWorld(Entity<XenoEggComponent> egg, ref ActivateInWorldEvent args)
    {
        // TODO CM14 multiple hive support
        if (!HasComp<XenoComponent>(args.User))
            return;

        if (Open(egg, args.User, out _))
            args.Handled = true;
    }

    private void OnXenoEggStepTriggerAttempt(Entity<XenoEggComponent> egg, ref StepTriggerAttemptEvent args)
    {
        if (CanTrigger(args.Tripper))
            args.Continue = true;
    }

    private void OnXenoEggStepTriggered(Entity<XenoEggComponent> egg, ref StepTriggeredOffEvent args)
    {
        if (egg.Comp.State != XenoEggState.Grown ||
            !CanTrigger(args.Tripper))
        {
            return;
        }

        if (Open(egg, args.Tripper, out var spawned) &&
            TryComp(spawned, out XenoHuggerComponent? hugger))
        {
            _hugger.Hug((spawned.Value, hugger), args.Tripper, force: true);
        }
    }

    private bool CanTrigger(EntityUid user)
    {
        return HasComp<HuggableComponent>(user) &&
               !HasComp<VictimHuggedComponent>(user) &&
               !_mobState.IsDead(user);
    }

    private bool Open(Entity<XenoEggComponent> egg, EntityUid? user, out EntityUid? spawned)
    {
        spawned = null;
        if (egg.Comp.State == XenoEggState.Opened)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-clear"), egg, user.Value);

            if (_net.IsClient)
                return true;

            QueueDel(egg);

            return true;
        }

        if (egg.Comp.State != XenoEggState.Grown)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("cm-xeno-egg-not-developed"), egg, user.Value);

            return false;
        }

        SetEggState(egg, XenoEggState.Opened);

        if (_net.IsClient)
            return true;

        if (TryComp(egg, out TransformComponent? xform))
        {
            spawned = SpawnAtPosition(egg.Comp.Spawn, xform.Coordinates);
            _xeno.SetHive(spawned.Value, egg.Comp.Hive);
        }

        return true;
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
            if (TryComp(egg, out XenoEggComponent? eggComp) &&
                TryComp(uid, out XenoComponent? xeno))
            {
                eggComp.Hive = xeno.Hive;
                Dirty(egg, eggComp);
            }

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
