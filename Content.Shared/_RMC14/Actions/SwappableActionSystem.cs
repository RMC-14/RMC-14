using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Actions;

public sealed class SwappableActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwappableActionComponent, AfterAutoHandleStateEvent>(OnSwappableHandleState);
    }

    private void OnSwappableHandleState(Entity<SwappableActionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.IsSwapped)
        {
            if (!HasComp<WorldTargetActionComponent>(ent) || ent.Comp.SwappedEventTemplate == null)
                return;

            _actions.SetEvent(ent, ent.Comp.SwappedEventTemplate);
        }
        else
        {
            if (!HasComp<InstantActionComponent>(ent) || ent.Comp.OriginalEventTemplate == null)
                return;

            _actions.SetEvent(ent, ent.Comp.OriginalEventTemplate);
        }
    }

    public bool SwapInstantToWorldTarget<TOriginalEvent>(
        EntityUid owner,
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

            if (!TryComp<SwappableActionComponent>(actionId, out var swappable) ||
                swappable.SwappedEventTemplate == null)
            {
                continue;
            }

            swappable.OriginalName = MetaData(actionId).EntityName;
            swappable.OriginalDescription = MetaData(actionId).EntityDescription;
            swappable.IsSwapped = true;
            Dirty(actionId, swappable);

            RemComp<InstantActionComponent>(actionId);

            var targetAction = EnsureComp<TargetActionComponent>(actionId);
            targetAction.CheckCanAccess = false;
            targetAction.Range = -1;
            targetAction.DeselectOnMiss = false;
            targetAction.Repeat = false;
            Dirty(actionId, targetAction);

            EnsureComp<WorldTargetActionComponent>(actionId);
            _actions.SetEvent(actionId, swappable.SwappedEventTemplate);

            _metaData.SetEntityName(actionId, swappedName);
            _metaData.SetEntityDescription(actionId, swappedDescription);

            return true;
        }

        return false;
    }

    public void SwapAllToInstant(EntityUid owner)
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

            if (swappable.OriginalEventTemplate == null)
                continue;

            RemComp<WorldTargetActionComponent>(actionId);
            RemComp<TargetActionComponent>(actionId);

            EnsureComp<InstantActionComponent>(actionId);
            _actions.SetEvent(actionId, swappable.OriginalEventTemplate);

            _metaData.SetEntityName(actionId, swappable.OriginalName);
            _metaData.SetEntityDescription(actionId, swappable.OriginalDescription);

            swappable.IsSwapped = false;
            Dirty(actionId, swappable);
        }
    }
}
