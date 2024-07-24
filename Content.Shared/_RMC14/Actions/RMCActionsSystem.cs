using Content.Shared.Actions;
using Content.Shared.Actions.Events;

namespace Content.Shared._RMC14.Actions;

public sealed class RMCActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private EntityQuery<ActionSharedCooldownComponent> _actionSharedCooldownQuery;

    public override void Initialize()
    {
        _actionSharedCooldownQuery = GetEntityQuery<ActionSharedCooldownComponent>();

        SubscribeLocalEvent<ActionSharedCooldownComponent, ActionPerformedEvent>(OnSharedCooldownPerformed);
    }

    private void OnSharedCooldownPerformed(Entity<ActionSharedCooldownComponent> ent, ref ActionPerformedEvent args)
    {
        foreach (var (actionId, _) in _actions.GetActions(args.Performer))
        {
            if (!_actionSharedCooldownQuery.TryComp(actionId, out var shared) ||
                shared.Id != ent.Comp.Id)
            {
                continue;
            }

            _actions.SetIfBiggerCooldown(actionId, ent.Comp.Cooldown);
        }
    }
}
