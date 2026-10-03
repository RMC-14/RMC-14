using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Actions;

public abstract class SharedRMCActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<ActionSharedCooldownComponent> _actionSharedCooldownQuery;

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _targetFactions = [];
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _userFactions = [];

    public override void Initialize()
    {
        _actionSharedCooldownQuery = GetEntityQuery<ActionSharedCooldownComponent>();

        SubscribeAllEvent<RMCMissedTargetActionEvent>(OnMissedTargetAction);

        SubscribeLocalEvent<ActionSharedCooldownComponent, ActionPerformedEvent>(OnSharedCooldownPerformed);

        SubscribeLocalEvent<ActionCooldownComponent, RMCActionUseEvent>(OnCooldownUse);

        SubscribeLocalEvent<ActionInRangeUnobstructedComponent, RMCActionUseAttemptEvent>(OnInRangeUnobstructedUseAttempt);

        SubscribeLocalEvent<ActionReducedUseDelayComponent, ActionReducedUseDelayEvent>(OnReducedUseDelayEvent);

        SubscribeLocalEvent<ActionReducedUseDelayComponent, StartUseDelayEvent>(OnReducedStartUseDelay);

        SubscribeLocalEvent<EntityTargetActionComponent, CheckActionTargetEvent>(OnCheckActionTarget);
        SubscribeLocalEvent<ActionTargetHiveAllyComponent, CheckActionTargetEvent>(OnHiveAllyCheckActionTarget);
        SubscribeLocalEvent<ActionTargetNonHiveAllyComponent, CheckActionTargetEvent>(OnNonHiveAllyCheckActionTarget);
        SubscribeLocalEvent<ActionTargetIFFComponent, CheckActionTargetEvent>(OnIFFCheckActionTarget);
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

    private void OnReducedUseDelayEvent(Entity<ActionReducedUseDelayComponent> ent, ref ActionReducedUseDelayEvent args)
    {
        if (args.Amount < 0 || args.Amount > 1 || args.Amount == ent.Comp.UseDelayReduction)
            return;

        ent.Comp.UseDelayReduction = args.Amount;

        Dirty(ent, ent.Comp);
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

    private void OnReducedStartUseDelay(Entity<ActionReducedUseDelayComponent> ent, ref StartUseDelayEvent args)
    {
        var reductionAmount = args.Delay * ent.Comp.UseDelayReduction.Float();
        args.Start -= reductionAmount;
        args.End -= reductionAmount;
    }

    private void OnCheckActionTarget(Entity<EntityTargetActionComponent> ent, ref CheckActionTargetEvent args)
    {
        if (args.Skip)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Target)
            || _whitelist.IsBlacklistPass(ent.Comp.Blacklist, args.Target)
            || !ent.Comp.CanTargetSelf && args.Target == args.User
            || args.Action.Comp.CheckCanInteract && !_actionBlocker.CanInteract(args.User, args.Target) && ent.Comp.TargetCheckCanInteract)
        {
            args.Skip = true;
        }
    }

    private void OnHiveAllyCheckActionTarget(Entity<ActionTargetHiveAllyComponent> ent, ref CheckActionTargetEvent args)
    {
        if (args.Skip || ent.Comp.Defer && args.Defer)
            return;

        if (_hive.FromSameHiveOrAlly(args.User, args.Target))
        {
            args.Skip = ent.Comp.Skip;
            args.Defer = ent.Comp.Defer;
        }
    }

    private void OnNonHiveAllyCheckActionTarget(Entity<ActionTargetNonHiveAllyComponent> ent, ref CheckActionTargetEvent args)
    {
        if (args.Skip || ent.Comp.Defer && args.Defer)
            return;

        if (!_hive.FromSameHiveOrAlly(args.User, args.Target))
        {
            args.Skip = ent.Comp.Skip;
            args.Defer = ent.Comp.Defer;
        }
    }

    private void OnIFFCheckActionTarget(Entity<ActionTargetIFFComponent> ent, ref CheckActionTargetEvent args)
    {
        if (args.Skip || ent.Comp.Defer && args.Defer)
            return;

        if (_gunIFF.TryGetFactions(args.User, _userFactions)
            && _gunIFF.TryGetFactions(args.Target, _targetFactions)
            && _userFactions.Overlaps(_targetFactions))
        {
            args.Skip = ent.Comp.Skip;
            args.Defer = ent.Comp.Defer;
        }
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

/// <summary>
/// Event raised on EntityTargetActionComponent that allows the action to decide if a target should be skipped or deferred to prefer other entities.
/// Handling this event should cause NO SIDE EFFECTS.
/// </summary>
/// <param name="Target">Target to check.</param>
/// <param name="User">User of the action.</param>
/// <param name="Action">The ActionComponent of the action.</param>
/// <param name="Skip">Set to true if the target should be skipped.</param>
/// <param name="Defer">Set to true if the target should be deferred.</param>
[ByRefEvent]
public record struct CheckActionTargetEvent(in EntityUid Target, in EntityUid User, in Entity<ActionComponent> Action, bool Skip = false, bool Defer = false);
