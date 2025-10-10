using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Interaction;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Actions;

public abstract class SharedRMCActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    private EntityQuery<ActionSharedCooldownComponent> _actionSharedCooldownQuery;

    public override void Initialize()
    {
        _actionSharedCooldownQuery = GetEntityQuery<ActionSharedCooldownComponent>();

        SubscribeAllEvent<RMCMissedTargetActionEvent>(OnMissedTargetAction);

        SubscribeLocalEvent<ActionSharedCooldownComponent, ActionPerformedEvent>(OnSharedCooldownPerformed);

        SubscribeLocalEvent<ActionCooldownComponent, RMCActionUseEvent>(OnCooldownUse);

        SubscribeLocalEvent<ActionInRangeUnobstructedComponent, RMCActionUseAttemptEvent>(OnInRangeUnobstructedUseAttempt);

        SubscribeLocalEvent<ActionComponent, ActionReducedUseDelayEvent>(OnReducedUseDelayEvent);
    }

    private void OnMissedTargetAction(RMCMissedTargetActionEvent args)
    {
        var action = GetEntity(args.Action);

        if (!TryComp(action, out RMCCooldownOnMissComponent? cooldown))
            return;

        _actions.SetIfBiggerCooldown(action, cooldown.MissCooldown);
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

    /// <summary>
    /// Enable all events that have a shared cooldown with the provided action
    /// </summary>
    public void EnableSharedCooldownEvents(Entity<ActionSharedCooldownComponent?> action, EntityUid performer)
    {
        SetStatusOfSharedCooldownEvents(action, performer, true);
    }

    /// <summary>
    /// Disable all events that have a shared cooldown with the provided action
    /// </summary>
    public void DisableSharedCooldownEvents(Entity<ActionSharedCooldownComponent?> action, EntityUid performer)
    {
        SetStatusOfSharedCooldownEvents(action, performer, false);
    }

    /// <summary>
    /// Sets the enabled status of all events that have a shared cooldown with the provided action
    /// </summary>
    private void SetStatusOfSharedCooldownEvents(Entity<ActionSharedCooldownComponent?> action, EntityUid performer, bool newStatus)
    {
        if (!Resolve(action, ref action.Comp, false))
            return;

        if (action.Comp.Cooldown == TimeSpan.Zero)
            return;

        foreach (var (actionId, comp) in _actions.GetActions(performer))
        {
            if (!_actionSharedCooldownQuery.TryComp(actionId, out var shared))
                continue;

            // Same ID or primary ID found in subset of other action's ids
            if (!(shared.Id != null && shared.Id == action.Comp.Id || action.Comp.Id != null &&
                  (shared.Ids.Contains(action.Comp.Id.Value) || shared.ActiveIds.Contains(action.Comp.Id.Value))))
            {
                continue;
            }

            _actions.SetEnabled((actionId, comp), newStatus);
        }
    }

    private void OnReducedUseDelayEvent(EntityUid uid, ActionComponent component, ActionReducedUseDelayEvent args)
    {
        if (!TryComp(uid, out ActionReducedUseDelayComponent? comp))
            return;

        if (args.Amount < 0 || args.Amount > 1)
            return;

        comp.UseDelayReduction = args.Amount;

        if (TryComp(uid, out ActionSharedCooldownComponent? shared))
        {
            comp.UseDelayBase ??= shared.Cooldown;

            RefreshSharedUseDelay((uid, comp), shared);
            return;
        }

        // Should be fine to only set this once as the base use delay should remain constant
        comp.UseDelayBase ??= component.UseDelay;
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
        _actions.SetIfBiggerCooldown(ent.Owner, ent.Comp.Cooldown);
    }

    private void OnInRangeUnobstructedUseAttempt(Entity<ActionInRangeUnobstructedComponent> ent, ref RMCActionUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target, ent.Comp.Range))
            args.Cancelled = true;
    }

    public bool CanUseActionPopup(EntityUid user, EntityUid action, EntityUid? target = null)
    {
        var ev = new RMCActionUseAttemptEvent(user, target);
        RaiseLocalEvent(action, ref ev);

        return !ev.Cancelled;
    }

    private void ActionUsed(EntityUid user, EntityUid action)
    {
        var ev = new RMCActionUseEvent(user);
        RaiseLocalEvent(action, ref ev);
    }

    public bool TryUseAction(EntityUid user, EntityUid action, EntityUid target)
    {
        if (!CanUseActionPopup(user, action, target))
            return false;

        ActionUsed(user, action);
        return true;
    }

    public bool TryUseAction(InstantActionEvent action)
    {
        if (!CanUseActionPopup(action.Performer, action.Action))
            return false;

        ActionUsed(action.Performer, action.Action);
        return true;
    }

    public bool TryUseAction(EntityTargetActionEvent action)
    {
        if (!CanUseActionPopup(action.Performer, action.Action, action.Target))
            return false;

        ActionUsed(action.Performer, action.Action);
        return true;
    }

    public bool TryUseAction(WorldTargetActionEvent action)
    {
        if (!CanUseActionPopup(action.Performer, action.Action))
            return false;

        ActionUsed(action.Performer, action.Action);
        return true;
    }

    public IEnumerable<Entity<ActionComponent>> GetActionsWithEvent<T>(EntityUid user) where T : BaseActionEvent
    {
        foreach (var action in _actions.GetActions(user))
        {
            if (_actions.GetEvent(action) is T)
                yield return action;
        }
    }

    public IEnumerable<Entity<ActionComponent, T>> GetActionsWithComp<T>(EntityUid user) where T : IComponent
    {
        foreach (var action in _actions.GetActions(user))
        {
            if (TryComp(action, out T? comp))
                yield return (action, action, comp);
        }
    }
}

[Serializable, NetSerializable]
public sealed class RMCMissedTargetActionEvent : EntityEventArgs
{
    public readonly NetEntity Action;
    public RMCMissedTargetActionEvent(NetEntity actionId)
    {
        Action = actionId;
    }
}
