using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared._CM14.Scoping;

public abstract partial class SharedScopeSystem
{
    public void InitializeUser()
    {
        SubscribeLocalEvent<ScopeUserComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<ScopeUserComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<ScopeUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ScopeUserComponent, EntityTerminatingEvent>(OnEntityTerminating);

        SubscribeLocalEvent<ScopeUserComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnMove(EntityUid uid, ScopeUserComponent component, ref MoveEvent args)
    {
        if (!TryComp<ScopeComponent>(component.ScopingItem, out var scopeComponent))
            return;

        if (!_transformSystem.InRange(scopeComponent.LastScopedAt,args.NewPosition, 1.5f))
            UserStopScoping(uid, component);
    }

    private void OnParentChanged(EntityUid uid, ScopeUserComponent component, ref EntParentChangedMessage args)
    {
        UserStopScoping(uid, component);
    }

    private void OnInsertAttempt(EntityUid uid, ScopeUserComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        UserStopScoping(uid, component);
    }

    private void OnEntityTerminating(EntityUid uid, ScopeUserComponent component, ref EntityTerminatingEvent args)
    {
        if (!TryComp<ScopeComponent>(component.ScopingItem, out var scopeComponent))
            return;

        StopScopingHelper(component.ScopingItem.Value, scopeComponent, uid);
    }

    private void OnPlayerDetached(EntityUid uid, ScopeUserComponent component, ref PlayerDetachedEvent args)
    {
        UserStopScoping(uid, component);
    }

    private void UserStopScoping(EntityUid uid, ScopeUserComponent component)
    {
        if (TryComp<ScopeComponent>(component.ScopingItem, out var scopeComponent) && scopeComponent.IsScoping)
            StopScoping(component.ScopingItem.Value, scopeComponent, uid);
    }
}
