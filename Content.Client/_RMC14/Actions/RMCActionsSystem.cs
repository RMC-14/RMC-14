using System.Linq;
using Content.Client.Actions;
using Content.Shared._RMC14.Actions;
using Content.Shared.Actions.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Actions;

public sealed class RMCActionsSystem : SharedRMCActionsSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityUid? _sortEnt;

    public void ActionsChanged(List<EntityUid?> actions)
    {
        var actionPrototypes = new List<EntProtoId>();
        foreach (var action in actions)
        {
            if (action == null || Prototype(action.Value) is not { } proto)
                continue;

            actionPrototypes.Add(proto);
        }

        var ev = new RMCActionOrderEvent(actionPrototypes);
        RaiseNetworkEvent(ev);
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (_sortEnt == player)
            return;

        _sortEnt = null;

        if (!TryComp(player, out RMCActionOrderComponent? orderComp) ||
            orderComp.Order is not { } order)
        {
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
