using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Actions;

public sealed class SwappableActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private readonly Dictionary<string, Func<WorldTargetActionEvent>> _eventFactories = new();
    private readonly Dictionary<string, Func<InstantActionEvent>> _instantEventFactories = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwappableActionComponent, AfterAutoHandleStateEvent>(OnSwappableHandleState);
    }

    public void RegisterEventFactory<TEvent>(Func<TEvent> factory) where TEvent : WorldTargetActionEvent
    {
        _eventFactories[typeof(TEvent).FullName!] = () => factory();
    }

    public void RegisterInstantEventFactory<TEvent>(Func<TEvent> factory) where TEvent : InstantActionEvent
    {
        _instantEventFactories[typeof(TEvent).FullName!] = () => factory();
    }

    private void OnSwappableHandleState(Entity<SwappableActionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.SwappedEventType != null)
        {
            if (!HasComp<WorldTargetActionComponent>(ent))
                return;

            if (!_eventFactories.TryGetValue(ent.Comp.SwappedEventType, out var factory))
                return;

            _actions.SetEvent(ent, factory());
        }
        else
        {
            if (!HasComp<InstantActionComponent>(ent))
                return;

            if (ent.Comp.OriginalEventType == null)
                return;

            if (!_instantEventFactories.TryGetValue(ent.Comp.OriginalEventType, out var factory))
                return;

            _actions.SetEvent(ent, factory());
        }
    }

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
            swappable.SwappedEventType = swappedEvent.GetType().FullName;
            swappable.OriginalEventType = typeof(TOriginalEvent).FullName;
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

            EnsureComp<InstantActionComponent>(actionId);
            _actions.SetEvent(actionId, originalEvent);

            _metaData.SetEntityName(actionId, swappable.OriginalName);
            _metaData.SetEntityDescription(actionId, swappable.OriginalDescription);

            swappable.SwappedEventType = null;
            Dirty(actionId, swappable);
        }
    }
}
