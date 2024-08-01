using Content.Shared._RMC14.Overwatch;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Overwatch;

public sealed class OverwatchConsoleSystem : SharedOverwatchConsoleSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OverwatchCameraComponent, ComponentRemove>(OnWatchedRemove);
        SubscribeLocalEvent<OverwatchCameraComponent, EntityTerminatingEvent>(OnWatchedRemove);
        SubscribeLocalEvent<OverwatchWatchingComponent, ComponentRemove>(OnWatchingRemove);
        SubscribeLocalEvent<OverwatchWatchingComponent, EntityTerminatingEvent>(OnWatchingRemove);
    }

    private void OnWatchedRemove<T>(Entity<OverwatchCameraComponent> ent, ref T args)
    {
        foreach (var watching in ent.Comp.Watching)
        {
            if (TerminatingOrDeleted(watching))
                continue;

            RemCompDeferred<OverwatchWatchingComponent>(watching);
        }
    }

    private void OnWatchingRemove<T>(Entity<OverwatchWatchingComponent> ent, ref T args)
    {
        RemoveWatcher(ent);
    }

    protected override void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<OverwatchCameraComponent?> toWatch)
    {
        base.Watch(watcher, toWatch);

        if (!Resolve(toWatch, ref toWatch.Comp, false))
            return;

        if (watcher.Owner == toWatch.Owner)
            return;

        if (!Resolve(watcher, ref watcher.Comp1, ref watcher.Comp2) ||
            !Resolve(toWatch, ref toWatch.Comp))
        {
            return;
        }

        _eye.SetTarget(watcher, toWatch, watcher);
        _viewSubscriber.AddViewSubscriber(toWatch, watcher.Comp1.PlayerSession);

        RemoveWatcher(watcher);
        EnsureComp<OverwatchWatchingComponent>(watcher).Watching = toWatch;
        toWatch.Comp.Watching.Add(watcher);
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        var oldTarget = watcher.Comp.Target;

        base.Unwatch(watcher, player);

        if (oldTarget != null && oldTarget != watcher.Owner)
            _viewSubscriber.RemoveViewSubscriber(oldTarget.Value, player);

        RemoveWatcher(watcher);
    }

    private void RemoveWatcher(EntityUid toRemove)
    {
        if (!TryComp(toRemove, out OverwatchWatchingComponent? watching))
            return;

        if (TryComp(watching.Watching, out OverwatchCameraComponent? watched))
            watched.Watching.Remove(toRemove);

        watching.Watching = null;
        RemCompDeferred<OverwatchWatchingComponent>(toRemove);
    }
}
