using Content.Shared.Actions;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Evolution;

public sealed class XenoEvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    private readonly HashSet<EntityUid> _climbable = new();
    private readonly HashSet<EntityUid> _doors = new();
    private readonly HashSet<EntityUid> _intersecting = new();

    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<XenoEvolveActionComponent> _xenoEvolveActionQuery;

    private readonly TimeSpan _cooldownThreshold = TimeSpan.FromSeconds(0.2);

    public override void Initialize()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _xenoEvolveActionQuery = GetEntityQuery<XenoEvolveActionComponent>();

        SubscribeLocalEvent<XenoEvolveActionComponent, MapInitEvent>(OnXenoEvolveActionMapInit);

        SubscribeLocalEvent<XenoEvolutionComponent, MapInitEvent>(OnXenoEvolveMapInit);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoOpenEvolutionsActionEvent>(OnXenoEvolveAction);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
        SubscribeLocalEvent<XenoEvolutionComponent, ActionAddedEvent>(OnXenoEvolveActionAdded);
        SubscribeLocalEvent<XenoEvolutionComponent, ActionRemovedEvent>(OnXenoEvolveActionRemoved);

        SubscribeLocalEvent<XenoNewlyEvolvedComponent, PreventCollideEvent>(OnNewlyEvolvedPreventCollide);

        Subs.BuiEvents<XenoEvolutionComponent>(XenoEvolutionUIKey.Key, subs =>
        {
            subs.Event<XenoEvolveBuiMsg>(OnXenoEvolveBui);
        });
    }

    private void OnXenoEvolveActionMapInit(Entity<XenoEvolveActionComponent> ent, ref MapInitEvent args)
    {
        if (_action.TryGetActionData(ent, out _, false))
            _action.SetCooldown(ent, _timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
    }

    private void OnXenoEvolveMapInit(Entity<XenoEvolutionComponent> ent, ref MapInitEvent args)
    {
        foreach (var (actionId, _) in _action.GetActions(ent))
        {
            if (_xenoEvolveActionQuery.HasComp(actionId) &&
                !ent.Comp.EvolutionActions.Contains(actionId))
            {
                ent.Comp.EvolutionActions.Add(actionId);
            }
        }
    }

    private void OnXenoEvolveAction(Entity<XenoEvolutionComponent> xeno, ref XenoOpenEvolutionsActionEvent args)
    {
        args.Handled = true;
        _ui.OpenUi(xeno.Owner, XenoEvolutionUIKey.Key, xeno);
    }

    private void OnXenoEvolveBui(Entity<XenoEvolutionComponent> xeno, ref XenoEvolveBuiMsg args)
    {
        var actor = args.Actor;
        if (!CanEvolvePopup(xeno, args.Choice))
        {
            Log.Warning($"{ToPrettyString(actor)} sent an invalid evolution choice: {args.Choice}.");
            return;
        }

        var ev = new XenoEvolutionDoAfterEvent(args.Choice);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.EvolutionDelay, ev, xeno);

        if (xeno.Comp.EvolutionDelay > TimeSpan.Zero)
            _popup.PopupClient(Loc.GetString("cm-xeno-evolution-start"), xeno, xeno);

        _doAfter.TryStartDoAfter(doAfter);
        _ui.CloseUi(xeno.Owner, XenoEvolutionUIKey.Key, actor);
    }

    private void OnXenoEvolveDoAfter(Entity<XenoEvolutionComponent> xeno, ref XenoEvolutionDoAfterEvent args)
    {
        if (_net.IsClient ||
            args.Handled ||
            args.Cancelled ||
            !_mind.TryGetMind(xeno, out var mindId, out _) ||
            !CanEvolvePopup(xeno, args.Choice))
        {
            return;
        }

        args.Handled = true;

        var coordinates = _transform.GetMoverCoordinates(xeno.Owner);
        var newXeno = Spawn(args.Choice, coordinates);
        _xeno.SetSameHive(newXeno, xeno.Owner);

        _mind.TransferTo(mindId, newXeno);
        _mind.UnVisit(mindId);

        // TODO CM14 this is a hack because climbing on a newly created entity does not work properly for the client
        var comp = EnsureComp<XenoNewlyEvolvedComponent>(newXeno);

        _doors.Clear();
        _entityLookup.GetEntitiesIntersecting(xeno, _doors);
        foreach (var id in _doors)
        {
            if (HasComp<DoorComponent>(id) || HasComp<AirlockComponent>(id))
                comp.StopCollide.Add(id);
        }

        var ev = new NewXenoEvolvedComponent(xeno);
        RaiseLocalEvent(newXeno, ref ev);

        Del(xeno.Owner);

        _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-end"), newXeno, newXeno);
    }

    private void OnXenoEvolveActionAdded(Entity<XenoEvolutionComponent> ent, ref ActionAddedEvent args)
    {
        if (_xenoEvolveActionQuery.HasComp(args.Action))
            ent.Comp.EvolutionActions.Add(args.Action);
    }

    private void OnXenoEvolveActionRemoved(Entity<XenoEvolutionComponent> ent, ref ActionRemovedEvent args)
    {
        ent.Comp.EvolutionActions.Remove(args.Action);
    }

    private void OnNewlyEvolvedPreventCollide(Entity<XenoNewlyEvolvedComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.StopCollide.Contains(args.OtherEntity))
            args.Cancelled = true;
    }

    private bool CanEvolvePopup(Entity<XenoEvolutionComponent> xeno, EntProtoId newXeno)
    {
        if (!xeno.Comp.EvolvesTo.Contains(newXeno))
            return false;

        if (!_prototypes.TryIndex(newXeno, out var prototype))
            return true;

        // TODO CM14 revive jelly when added should not bring back dead queens
        if (prototype.TryGetComponent(out XenoEvolutionCappedComponent? capped, _compFactory) &&
            HasAlive<XenoEvolutionCappedComponent>(capped.Max, e => e.Comp.Id == capped.Id))
        {
            _popup.PopupClient($"There already is a living {prototype.Name}!", xeno, xeno, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private bool HasAlive<T>(int count, Predicate<Entity<T>>? predicate = null) where T : IComponent
    {
        if (count <= 0)
            return true;

        var total = 0;
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobStateQuery.TryComp(uid, out var mobState) &&
                _mobState.IsDead(uid, mobState))
            {
                continue;
            }

            if (predicate != null && !predicate((uid, comp)))
                continue;

            total++;

            if (total >= count)
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        var newly = EntityQueryEnumerator<XenoNewlyEvolvedComponent>();
        while (newly.MoveNext(out var uid, out var comp))
        {
            if (comp.TriedClimb)
            {
                _intersecting.Clear();
                _entityLookup.GetEntitiesIntersecting(uid, _intersecting);
                for (var i = comp.StopCollide.Count - 1; i >= 0; i--)
                {
                    var colliding = comp.StopCollide[i];
                    if (!_intersecting.Contains(colliding))
                        comp.StopCollide.RemoveAt(i);
                }

                if (comp.StopCollide.Count == 0)
                    RemCompDeferred<XenoNewlyEvolvedComponent>(uid);

                continue;
            }

            comp.TriedClimb = true;
            if (TryComp(uid, out ClimbingComponent? climbing))
            {
                _climbable.Clear();
                _entityLookup.GetEntitiesIntersecting(uid, _climbable);

                foreach (var intersecting in _climbable)
                {
                    if (HasComp<ClimbableComponent>(intersecting))
                    {
                        _climb.ForciblySetClimbing(uid, intersecting);
                        Dirty(uid, climbing);
                        break;
                    }
                }
            }
        }

        // TODO CM14 ovipositor attached only after 5 minutes
        var hasGranter = HasAlive<XenoEvolutionGranterComponent>(1);
        var add = TimeSpan.FromSeconds(frameTime);
        if (!hasGranter && _net.IsServer)
        {
            var evolution = EntityQueryEnumerator<XenoEvolutionComponent>();
            while (evolution.MoveNext(out var xeno, out var comp))
            {
                if (!comp.RequiresGranter)
                    continue;

                if (_mobStateQuery.TryComp(xeno, out var mobState) &&
                    _mobState.IsDead(xeno, mobState))
                {
                    continue;
                }

                for (var i = comp.EvolutionActions.Count - 1; i >= 0; i--)
                {
                    var action = comp.EvolutionActions[i];
                    if (!_action.TryGetActionData(action, out var actionComp) ||
                        !_xenoEvolveActionQuery.TryComp(action, out var evolveAction))
                    {
                        comp.EvolutionActions.RemoveAt(i);
                        continue;
                    }

                    if (actionComp.Cooldown is not { } cooldown)
                    {
                        if (evolveAction.CooldownAccumulated != TimeSpan.Zero)
                        {
                            evolveAction.CooldownAccumulated = TimeSpan.Zero;
                            Dirty(action, evolveAction);
                        }

                        continue;
                    }

                    // this is a very convoluted way of not calling dirty every tick
                    // as to not kill the server
                    evolveAction.CooldownAccumulated += add;
                    if (evolveAction.CooldownAccumulated >= _cooldownThreshold)
                    {
                        var accumulated = evolveAction.CooldownAccumulated;
                        evolveAction.CooldownAccumulated = TimeSpan.Zero;
                        Dirty(action, evolveAction);

                        _action.SetCooldown(action, cooldown.Start + accumulated, cooldown.End + accumulated);
                    }
                }
            }
        }
    }
}
