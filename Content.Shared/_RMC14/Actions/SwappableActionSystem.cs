using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Actions;

public sealed class SwappableActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private readonly Dictionary<SwappableActionEvent, Func<WorldTargetActionEvent>> _eventFactories = new();
    private readonly Dictionary<SwappableActionEvent, Func<InstantActionEvent>> _instantEventFactories = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwappableActionComponent, AfterAutoHandleStateEvent>(OnSwappableHandleState);
    }

    public void RegisterEventFactory(SwappableActionEvent key, Func<WorldTargetActionEvent> factory)
    {
        _eventFactories[key] = factory;
    }

    public void RegisterInstantEventFactory(SwappableActionEvent key, Func<InstantActionEvent> factory)
    {
        _instantEventFactories[key] = factory;
    }

    private void OnSwappableHandleState(Entity<SwappableActionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.SwappedEvent != SwappableActionEvent.None)
        {
            if (!HasComp<WorldTargetActionComponent>(ent))
                return;

            if (!_eventFactories.TryGetValue(ent.Comp.SwappedEvent, out var factory))
                return;

            _actions.SetEvent(ent, factory());
        }
        else
        {
            if (!HasComp<InstantActionComponent>(ent))
                return;

            if (ent.Comp.OriginalEvent == SwappableActionEvent.None)
                return;

            if (!_instantEventFactories.TryGetValue(ent.Comp.OriginalEvent, out var factory))
                return;

            _actions.SetEvent(ent, factory());
        }
    }

    public bool SwapInstantToWorldTarget<TOriginalEvent>(
        EntityUid owner,
        WorldTargetActionEvent swappedEvent,
        SwappableActionEvent swappedKey,
        SwappableActionEvent originalKey,
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
            swappable.SwappedEvent = swappedKey;
            swappable.OriginalEvent = originalKey;
            Dirty(actionId, swappable);

            RemComp<InstantActionComponent>(actionId);

            var targetAction = EnsureComp<TargetActionComponent>(actionId);
            targetAction.CheckCanAccess = false;
            targetAction.Range = -1;
            targetAction.DeselectOnMiss = false;
            targetAction.Repeat = false;
            Dirty(actionId, targetAction);

            EnsureComp<WorldTargetActionComponent>(actionId);
            _actions.SetEvent(actionId, swappedEvent);

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

            if (!_instantEventFactories.TryGetValue(swappable.OriginalEvent, out var factory))
                continue;

            RemComp<WorldTargetActionComponent>(actionId);
            RemComp<TargetActionComponent>(actionId);

            EnsureComp<InstantActionComponent>(actionId);
            _actions.SetEvent(actionId, factory());

            _metaData.SetEntityName(actionId, swappable.OriginalName);
            _metaData.SetEntityDescription(actionId, swappable.OriginalDescription);

            swappable.SwappedEvent = SwappableActionEvent.None;
            Dirty(actionId, swappable);
        }
    }
}
