using Content.Shared._RMC14.Ladder;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Ladder;

public sealed class LadderSystem : SharedLadderSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LadderWatchingComponent, ComponentRemove>(OnWatchingRemove);
        SubscribeLocalEvent<LadderWatchingComponent, EntityTerminatingEvent>(OnWatchingRemove);
    }

    private void OnWatchingRemove<T>(Entity<LadderWatchingComponent> ent, ref T args)
    {
        RemoveWatcher(ent);
    }

    protected override void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<LadderComponent?> toWatch)
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
        EnsureComp<LadderWatchingComponent>(watcher).Watching = toWatch;
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
        if (!TryComp(toRemove, out LadderWatchingComponent? watching))
            return;

        if (TryComp(watching.Watching, out LadderComponent? watched))
            watched.Watching.Remove(toRemove);

        watching.Watching = null;
        RemCompDeferred<LadderWatchingComponent>(toRemove);
    }

    protected override void AddViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
        base.AddViewer(ent, player);
        _viewSubscriber.AddViewSubscriber(ent, player);
    }

    protected override void RemoveViewer(Entity<LadderComponent> ent, ICommonSession player)
    {
        base.RemoveViewer(ent, player);
        _viewSubscriber.RemoveViewSubscriber(ent, player);
    }
}
