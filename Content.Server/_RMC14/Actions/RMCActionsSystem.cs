using Content.Server.Actions;
using Content.Shared._RMC14.Actions;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Actions;

public sealed class RMCActionsSystem : SharedRMCActionsSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly RMCActionsManager _manager = default!;

    private readonly HashSet<EntProtoId> _actionsPresent = new();
    private readonly Dictionary<(NetUserId User, EntProtoId Id), List<EntProtoId>> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RMCActionOrderEvent>(OnActionOrder);

        SubscribeLocalEvent<RMCActionOrderComponent, PlayerAttachedEvent>(OnOrderAttached);
    }

    private void OnActionOrder(RMCActionOrderEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent)
            return;

        if (!TryComp(ent, out RMCActionOrderComponent? order) ||
            string.IsNullOrWhiteSpace(order.Id))
        {
            return;
        }

        _actionsPresent.Clear();
        foreach (var action in _actions.GetActions(ent))
        {
            if (Prototype(action)?.ID is not { } proto)
                continue;

            _actionsPresent.Add(proto);
        }

        for (var i = msg.Actions.Count - 1; i >= 0; i--)
        {
            var action = msg.Actions[i];
            if (!_actionsPresent.Contains(action))
                msg.Actions.RemoveAt(i);
        }

        _toUpdate[(args.SenderSession.UserId, order.Id)] = msg.Actions;
    }

    private void OnOrderAttached(Entity<RMCActionOrderComponent> ent, ref PlayerAttachedEvent args)
    {
        ent.Comp.Order = _manager.GetOrder(args.Player.UserId, ent.Comp.Id);
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        try
        {
            foreach (var ((user, id), actions) in _toUpdate)
            {
                try
                {
                    _manager.SetOrder(user, id, actions);
                }
                catch (Exception e)
                {
                    Log.Error($"Error saving action order for {user}:\n{e}");
                }
            }
        }
        finally
        {
            _toUpdate.Clear();
        }
    }
}
