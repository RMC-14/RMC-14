using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Actions;

public sealed class SwappableActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwappableActionComponent, AfterAutoHandleStateEvent>(OnSwappableHandleState);
    }

    private void OnSwappableHandleState(Entity<SwappableActionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.IsSwapped)
        {
            if (HasComp<WorldTargetActionComponent>(ent) &&
                ent.Comp.SwappedActionProto is { } id &&
                _prototype.TryIndex(id, out var proto) &&
                proto.TryGetComponent<WorldTargetActionComponent>(out var world, _compFactory) &&
                world.Event is { } ev)
            {
                _actions.SetEvent(ent, ev);
            }
        }
        else
        {
            if (HasComp<InstantActionComponent>(ent) &&
                MetaData(ent).EntityPrototype is { } proto &&
                proto.TryGetComponent<InstantActionComponent>(out var instant, _compFactory) &&
                instant.Event is { } ev)
            {
                _actions.SetEvent(ent, ev);
            }
        }
    }

    public bool SwapInstantToWorldTarget<TOriginalEvent>(EntityUid owner)
        where TOriginalEvent : InstantActionEvent
    {
        foreach (var (actionId, _) in _actions.GetActions(owner))
        {
            if (!TryComp<InstantActionComponent>(actionId, out var instant) ||
                instant.Event is not TOriginalEvent)
            {
                continue;
            }

            if (!TryComp<SwappableActionComponent>(actionId, out var swappable) ||
                swappable.SwappedActionProto is not { } protoId ||
                !_prototype.TryIndex(protoId, out var proto))
            {
                continue;
            }

            swappable.IsSwapped = true;
            Dirty(actionId, swappable);

            SwapActionTo(actionId, proto);
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

            if (!swappable.IsSwapped ||
                MetaData(actionId).EntityPrototype is not { } originalProto)
            {
                continue;
            }

            SwapActionTo(actionId, originalProto);

            swappable.IsSwapped = false;
            Dirty(actionId, swappable);
        }
    }

    /// <summary>
    ///     Swaps the action to the target action prototype
    /// </summary>
    private void SwapActionTo(EntityUid actionId, EntityPrototype proto)
    {
        _metaData.SetEntityName(actionId, proto.Name);
        _metaData.SetEntityDescription(actionId, proto.Description);

        if (proto.TryGetComponent<ActionComponent>(out var protoAction, _compFactory))
        {
            _actions.SetUseDelay(actionId, protoAction.UseDelay);
            _actions.SetIcon(actionId, protoAction.Icon);
            _actions.SetIconOn(actionId, protoAction.IconOn);
            _actions.SetIconColor(actionId, protoAction.IconColor);
        }

        if (proto.TryGetComponent<WorldTargetActionComponent>(out var protoWorld, _compFactory))
        {
            RemComp<InstantActionComponent>(actionId);

            var target = EnsureComp<TargetActionComponent>(actionId);
            if (proto.TryGetComponent<TargetActionComponent>(out var protoTarget, _compFactory))
            {
                target.Repeat = protoTarget.Repeat;
                target.DeselectOnMiss = protoTarget.DeselectOnMiss;
                target.CheckCanAccess = protoTarget.CheckCanAccess;
                target.Range = protoTarget.Range;
            }
            Dirty(actionId, target);

            var world = EnsureComp<WorldTargetActionComponent>(actionId);
            world.RotateOnUse = protoWorld.RotateOnUse;
            Dirty(actionId, world);

            if (protoWorld.Event is { } worldEv)
                _actions.SetEvent(actionId, worldEv);
        }
        else if (proto.TryGetComponent<InstantActionComponent>(out var protoInstant, _compFactory))
        {
            RemComp<WorldTargetActionComponent>(actionId);
            RemComp<TargetActionComponent>(actionId);

            EnsureComp<InstantActionComponent>(actionId);
            if (protoInstant.Event is { } instantEv)
                _actions.SetEvent(actionId, instantEv);
        }
    }
}
