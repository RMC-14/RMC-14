using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Actions;

public sealed class RMCActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private EntityQuery<ActionSharedCooldownComponent> _actionSharedCooldownQuery;

    public override void Initialize()
    {
        _actionSharedCooldownQuery = GetEntityQuery<ActionSharedCooldownComponent>();

        SubscribeLocalEvent<ActionSharedCooldownComponent, ActionPerformedEvent>(OnSharedCooldownPerformed);

        SubscribeLocalEvent<ActionCooldownComponent, RMCActionUseEvent>(OnCooldownUse);
        
        SubscribeLocalEvent<InstantActionComponent, ActionReducedUseDelayEvent>(OnReducedUseDelayEvent);
        SubscribeLocalEvent<EntityTargetActionComponent, ActionReducedUseDelayEvent>(OnReducedUseDelayEvent);
        SubscribeLocalEvent<WorldTargetActionComponent, ActionReducedUseDelayEvent>(OnReducedUseDelayEvent);
        SubscribeLocalEvent<EntityWorldTargetActionComponent, ActionReducedUseDelayEvent>(OnReducedUseDelayEvent);
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

    private void OnReducedUseDelayEvent<T>(EntityUid uid, T component, ActionReducedUseDelayEvent args) where T : BaseActionComponent
    {
        if (!TryComp(uid, out ActionReducedUseDelayComponent? comp))
            return;

        if (args.Amount < 0 || args.Amount > 1)
            return;

        comp.UseDelayReduction = args.Amount;

        if (TryComp(uid, out ActionSharedCooldownComponent? shared))
        {
            if (comp.UseDelayBase == null)
                comp.UseDelayBase = shared.Cooldown;

            RefreshSharedUseDelay((uid, comp), shared);
            return;
        }

        // Should be fine to only set this once as the base use delay should remain constant
        if (comp.UseDelayBase == null)
            comp.UseDelayBase = component.UseDelay;

        RefreshUseDelay((uid, comp));
    }

    private void RefreshUseDelay(Entity<ActionReducedUseDelayComponent> ent)
    {
        if (ent.Comp.UseDelayBase is not { } delayBase)
            return;

        var reduction = ent.Comp.UseDelayReduction.Double();
        var delayNew = delayBase.Multiply(1 - reduction);

        _actions.SetUseDelay(ent.Owner, delayNew);
    }

    private void RefreshSharedUseDelay(Entity<ActionReducedUseDelayComponent> ent, ActionSharedCooldownComponent shared)
    {
        if (ent.Comp.UseDelayBase is not { } delayBase)
            return;

        var reduction = ent.Comp.UseDelayReduction.Double();
        var delayNew = delayBase.Multiply(1 - reduction);

        shared.Cooldown = delayNew;
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
