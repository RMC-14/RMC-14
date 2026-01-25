using System.Linq;
using Content.Client.Actions;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Actions;

public sealed class RMCActionsSystem : SharedRMCActionsSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityUid? _sortEnt;

    public override void Initialize()
    {
        base.Initialize();
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
