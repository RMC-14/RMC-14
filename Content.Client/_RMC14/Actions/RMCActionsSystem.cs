using System.Linq;
using Content.Client._RMC14.Movement;
using Content.Client._RMC14.Xenonids.Hive;
using Content.Client.Actions;
using Content.Client.Gameplay;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Whitelist;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Prototypes;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client._RMC14.Actions;

public sealed class RMCActionsSystem : SharedRMCActionsSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly XenoHiveSystem _hive = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly RMCLagCompensationSystem _rmcLag = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<ActionComponent> _actionQuery;

    private EntityUid? _sortEnt;

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _targetFactions = [];
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _userFactions = [];

    public override void Initialize()
    {
        base.Initialize();
        _actionQuery = GetEntityQuery<ActionComponent>();
        SubscribeNetworkEvent<RMCActionOrderLoadedEvent>(OnActionOrderLoaded);
    }

    private void OnActionOrderLoaded(RMCActionOrderLoadedEvent ev)
    {
        // Re-trigger reordering
        _sortEnt = null;
    }

    public void ActionsChanged(List<EntityUid?> actions)
    {
        var actionPrototypes = new List<EntProtoId>();
        foreach (var action in actions)
        {
            if (action == null || Prototype(action.Value) is not { } proto)
                continue;

            actionPrototypes.Add(proto);
        }

        var ev = new RMCActionOrderChangeEvent(actionPrototypes);
        RaiseNetworkEvent(ev);
    }

    public EntityUid? GetActionTarget(EntityUid user, Entity<ActionComponent?> action, in PointerInputCmdArgs args)
    {
        if (!Resolve(action, ref action.Comp))
            return null;

        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        if (!EntityManager.TryGetComponent<EntityTargetActionComponent>(action, out var comp))
            return null;

        var coords = _eyeManager.ScreenToMap(args.ScreenCoordinates);

        var clickables = screen.GetClickableEntities(coords);

        EntityUid? firstTarget = null;
        EntityUid? firstPriorityTarget = null;

        foreach (var clickable in clickables)
        {
            if (_whitelist.IsWhitelistFail(comp.Whitelist, clickable))
                continue;

            if (_whitelist.IsBlacklistPass(comp.Blacklist, clickable))
                continue;

            if (!comp.CanTargetSelf && clickable == user)
                continue;

            if (action.Comp.CheckCanInteract && !_actionBlocker.CanInteract(user, clickable) && comp.TargetCheckCanInteract)
                continue;

            // TODO RMC14 This part is awful. Unsure how easy it would be to make reasonable without tearing up the existing actions system.
            // Many actions use ActionValidateEvent to validate if they can actually be performed (against the target or otherwise).
            // To use that event, we need to make a fake RequestPerformActionEvent that would match what would actually be used when using the action.
            // In short, we try to test if the action would be valid against the given clickable if it was to actually run. If not, we ignore that clickable.
            var expectedRequest = new RequestPerformActionEvent(GetNetEntity(action), GetNetEntity(clickable), GetNetCoordinates(args.Coordinates), _rmcLag.GetLastRealTick(null));
            var provider = action.Comp.Container ?? user;
            var validateEv = new ActionValidateEvent()
            {
                Input = expectedRequest,
                User = user,
                Provider = provider
            };
            RaiseLocalEvent(action, ref validateEv);
            if (validateEv.Invalid)
                continue;

            firstTarget ??= clickable;

            if (comp.DeferSharedIFFFaction
                && _gunIFF.TryGetFactions(user, _userFactions)
                && _gunIFF.TryGetFactions(clickable, _targetFactions)
                && _userFactions.Overlaps(_targetFactions))
            {
                continue;
            }

            if (comp.DeferHiveAllies
                && _hive.FromSameHiveOrAlly(user, clickable))
            {
                continue;
            }

            firstPriorityTarget ??= clickable;
            break;
        }

        return firstPriorityTarget ?? firstTarget;
    }

    private void SortDefault(EntityUid player)
    {
        if (!TryComp(player, out XenoComponent? xeno))
            return;

        foreach (var (_, actionId) in xeno.Actions)
        {
            if (!actionId.IsValid())
                return;
        }

        _sortEnt = player;

        var actions = new List<Entity<ActionComponent>>();
        foreach (var action in _actions.GetActions(player))
        {
            actions.Add(action);
        }

        var xenoActions = xeno.Actions.Values.ToList();
        actions.Sort((a, b) =>
        {
            var aXeno = xenoActions.FindIndex(e => e == a.Owner);
            var bXeno = xenoActions.FindIndex(e => e == b.Owner);
            if (aXeno != -1 && bXeno != -1)
                return aXeno - bXeno;

            return ActionsSystem.ActionComparer((a, a), (b, b));
        });

        var assignments = actions.Select((t, i) => new ActionsSystem.SlotAssignment(0, (byte) i, t)).ToList();
        _actions.SetAssignments(assignments);
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (_sortEnt == player)
            return;

        _sortEnt = null;

        if (!TryComp(player, out RMCActionOrderComponent? orderComp) ||
            orderComp.Order is not { Length: > 0 } order)
        {
            SortDefault(player);
            return;
        }

        var clientActions = _actions.GetClientActions().ToArray();
        foreach (var action in clientActions)
        {
            if (!action.Owner.IsValid())
                return;
        }

        _sortEnt = player;

        var actions = new Entity<ActionComponent>[order.Length];
        var extraActions = new List<Entity<ActionComponent>>();
        foreach (var action in clientActions)
        {
            var prototype = Prototype(action)?.ID;
            if (prototype == null)
            {
                extraActions.Add(action);
                continue;
            }

            var index = order.IndexOf(prototype);
            if (index < 0)
            {
                extraActions.Add(action);
                continue;
            }

            actions[index] = action;
        }

        var assignments = new List<ActionsSystem.SlotAssignment>();
        var allActions = actions.Concat(extraActions).Where(a => a != default).ToArray();
        for (var i = 0; i < allActions.Length; i++)
        {
            assignments.Add(new ActionsSystem.SlotAssignment(0, (byte) i, allActions[i]));
        }

        _actions.SetAssignments(assignments);
    }
}
