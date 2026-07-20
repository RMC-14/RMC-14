using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;

namespace Content.Shared._RMC14.Chat;

public sealed class CommandSpeechSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SquadLeaderCommandSpeechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SquadLeaderCommandSpeechComponent, ComponentRemove>(OnSquadLeaderCommandSpeechRemoved);
        SubscribeLocalEvent<SquadLeaderCommandSpeechComponent, CommandSpeechActionEvent>(OnSquadLeaderCommandSpeechAction);

        SubscribeLocalEvent<InnateCommandSpeechComponent, MapInitEvent>(OnInnateMapInit);
        SubscribeLocalEvent<InnateCommandSpeechComponent, ComponentRemove>(OnInnateCommandSpeechRemoved);
        SubscribeLocalEvent<InnateCommandSpeechComponent, CommandSpeechActionEvent>(OnInnateCommandSpeechAction);
    }

    private void OnMapInit(Entity<SquadLeaderCommandSpeechComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(
            ent.Owner,
            ref ent.Comp.CommandSpeechAction,
            ent.Comp.CommandSpeechActionId);
    }

    private void OnInnateMapInit(Entity<InnateCommandSpeechComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(
            ent.Owner,
            ref ent.Comp.CommandSpeechAction,
            ent.Comp.CommandSpeechActionId);
    }

    private void OnSquadLeaderCommandSpeechRemoved(Entity<SquadLeaderCommandSpeechComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.CommandSpeechAction is { } action)
            _actions.RemoveAction(ent.Owner, action);
    }

    private void OnInnateCommandSpeechRemoved(Entity<InnateCommandSpeechComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.CommandSpeechAction is { } action)
            _actions.RemoveAction(ent.Owner, action);
    }

    private void OnSquadLeaderCommandSpeechAction(Entity<SquadLeaderCommandSpeechComponent> ent, ref CommandSpeechActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Active = !ent.Comp.Active;
        Dirty(ent);

        foreach (var action in _rmcActions.GetActionsWithEvent<CommandSpeechActionEvent>(ent))
        {
            _actions.SetToggled((action, action), ent.Comp.Active);
        }
    }

    private void OnInnateCommandSpeechAction(Entity<InnateCommandSpeechComponent> ent, ref CommandSpeechActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Active = !ent.Comp.Active;
        Dirty(ent);

        foreach (var action in _rmcActions.GetActionsWithEvent<CommandSpeechActionEvent>(ent))
        {
            _actions.SetToggled((action, action), ent.Comp.Active);
        }
    }
}
