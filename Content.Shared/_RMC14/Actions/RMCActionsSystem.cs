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

        SubscribeLocalEvent<ActionCooldownComponent, RMCActionUseEvent
        >(OnCooldownUse);
    }

    private void OnSharedCooldownPerformed(Entity<ActionSharedCooldownComponent> ent, ref ActionPerformedEvent args)
    {
        if (ent.Comp.OnPerform)
            ActivateSharedCooldown((ent, ent), args.Performer);
    }

    public void ActivateSharedCooldown(Entity<ActionSharedCooldownComponent?> action, EntityUid performer)
    {
        if (!Resolve(action, ref action.Comp, false))
            return;

        if (action.Comp.Cooldown == TimeSpan.Zero)
            return;

        foreach (var (actionId, _) in _actions.GetActions(performer))
        {
            if (!_actionSharedCooldownQuery.TryComp(actionId, out var shared))
                continue;

            // Same ID or primary ID found in subset of other action's ids
            if ((shared.Id != null && shared.Id == action.Comp.Id) || (action.Comp.Id != null && shared.Ids.Contains(action.Comp.Id.Value)))
                _actions.SetIfBiggerCooldown(actionId, action.Comp.Cooldown);
        }
    }

    private void OnCooldownUse(Entity<ActionCooldownComponent> ent, ref RMCActionUseEvent args)
    {
        _actions.SetIfBiggerCooldown(ent, ent.Comp.Cooldown);
    }

    public bool CanUseActionPopup(EntityUid user, EntityUid action)
    {
        var ev = new RMCActionUseAttemptEvent(user);
        RaiseLocalEvent(action, ref ev);
        return !ev.Cancelled;
    }

    public void ActionUsed(EntityUid user, EntityUid action)
    {
        var ev = new RMCActionUseEvent(user);
        RaiseLocalEvent(action, ref ev);
    }

    public bool TryUseAction(EntityUid user, EntityUid action)
    {
        if (!CanUseActionPopup(user, action))
            return false;

        ActionUsed(user, action);
        return true;
    }
}
