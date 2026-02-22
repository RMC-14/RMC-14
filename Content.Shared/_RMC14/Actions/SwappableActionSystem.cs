using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Shared._RMC14.Actions;

// Swaps between instant and WorldTarget actions whilst keeping action bar position.
public sealed class SwappableActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public bool SwapInstantToWorldTarget<TOriginalEvent>(
        EntityUid owner,
        WorldTargetActionEvent swappedEvent,
        string swappedName,
        string swappedDescription) where TOriginalEvent : InstantActionEvent
    {
        foreach (var (actionId, _) in _actions.GetActions(owner))
        {
            if (!TryComp<InstantActionComponent>(actionId, out var instant) ||
                instant.Event is not TOriginalEvent)
            {
                continue;
            }

            var swappable = EnsureComp<SwappableActionComponent>(actionId);
            swappable.OriginalName = MetaData(actionId).EntityName;
            swappable.OriginalDescription = MetaData(actionId).EntityDescription;
            Dirty(actionId, swappable);

            RemComp<InstantActionComponent>(actionId);

            var targetAction = EnsureComp<TargetActionComponent>(actionId);
            targetAction.CheckCanAccess = false;
            targetAction.Range = -1;
            targetAction.DeselectOnMiss = false;
            targetAction.Repeat = false;
            Dirty(actionId, targetAction);

            var worldTarget = EnsureComp<WorldTargetActionComponent>(actionId);
            worldTarget.Event = swappedEvent;
            Dirty(actionId, worldTarget);

            _metaData.SetEntityName(actionId, swappedName);
            _metaData.SetEntityDescription(actionId, swappedDescription);

            return true;
        }

        return false;
    }

    public void SwapAllToInstant(EntityUid owner, InstantActionEvent originalEvent)
    {
        foreach (var (actionId, _) in _actions.GetActions(owner))
        {
            if (!TryComp<SwappableActionComponent>(actionId, out var swappable))
                continue;

            if (TerminatingOrDeleted(actionId))
            {
                RemCompDeferred<SwappableActionComponent>(actionId);
                continue;
            }

            RemComp<WorldTargetActionComponent>(actionId);
            RemComp<TargetActionComponent>(actionId);

            var instant = EnsureComp<InstantActionComponent>(actionId);
            instant.Event = originalEvent;

            _metaData.SetEntityName(actionId, swappable.OriginalName);
            _metaData.SetEntityDescription(actionId, swappable.OriginalDescription);

            RemComp<SwappableActionComponent>(actionId);
        }
    }
}
