using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;

namespace Content.Shared._RMC14.Chat;

public sealed class CommandSpeechSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InnateCommandSpeechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InnateCommandSpeechComponent, ComponentRemove>(OnCommandSpeechRemoved);
        SubscribeLocalEvent<InnateCommandSpeechComponent, CommandSpeechActionEvent>(OnCommandSpeechAction);
    }

    private void OnMapInit(Entity<InnateCommandSpeechComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(
            ent.Owner,
            ref ent.Comp.CommandSpeechAction,
            ent.Comp.CommandSpeechActionId);
    }

    private void OnCommandSpeechRemoved(Entity<InnateCommandSpeechComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.CommandSpeechAction is { } action)
            _actions.RemoveAction(ent.Owner, action);
    }

    private void OnCommandSpeechAction(Entity<InnateCommandSpeechComponent> ent, ref CommandSpeechActionEvent args)
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
