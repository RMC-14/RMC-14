using Content.Shared._RMC14.Camera;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Camera;

public sealed class RMCCameraSystem : SharedRMCCameraSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private EntityQuery<ActorComponent> _actorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeLocalEvent<RMCCameraWatcherComponent, PlayerAttachedEvent>(OnWatcherPlayerAttached);
        SubscribeLocalEvent<RMCCameraWatcherComponent, PlayerDetachedEvent>(OnWatcherPlayerDetached);
    }

    private void OnWatcherPlayerAttached(Entity<RMCCameraWatcherComponent> ent, ref PlayerAttachedEvent args)
    {
        foreach (var netOverride in ent.Comp.Overrides)
        {
            if (TryGetEntity(netOverride, out var over))
                _viewSubscriber.AddViewSubscriber(over.Value, args.Player);
        }
    }

    private void OnWatcherPlayerDetached(Entity<RMCCameraWatcherComponent> ent, ref PlayerDetachedEvent args)
    {
        foreach (var netOverride in ent.Comp.Overrides)
        {
            if (TryGetEntity(netOverride, out var over))
                _viewSubscriber.RemoveViewSubscriber(over.Value, args.Player);
        }
    }

    protected override void Refresh(Entity<RMCCameraComputerComponent> ent, EntityUid? old)
    {
        base.Refresh(ent, old);

        for (var i = ent.Comp.Watchers.Count - 1; i >= 0; i--)
        {
            var watcher = ent.Comp.Watchers[i];
            if (TerminatingOrDeleted(watcher))
            {
                ent.Comp.Watchers.RemoveAt(i);
                continue;
            }

            if (!_actorQuery.TryComp(watcher, out var actor))
                continue;

            RMCCameraWatcherComponent? watcherComp = null;
            if (old != null && TryComp(watcher, out watcherComp))
                RemoveOverrides((watcher, watcherComp, actor));

            if (ent.Comp.CurrentCamera is not { } current)
                continue;

            _viewSubscriber.AddViewSubscriber(current, actor.PlayerSession);

            watcherComp ??= EnsureComp<RMCCameraWatcherComponent>(watcher);
            watcherComp.Overrides.Add(GetNetEntity(current));
            Dirty(watcher, watcherComp);
        }
    }

    protected override void OnWatcherRemoved(Entity<RMCCameraWatcherComponent> watcher)
    {
        base.OnWatcherRemoved(watcher);
        RemoveOverrides(watcher);
    }

    private void RemoveOverrides(Entity<RMCCameraWatcherComponent, ActorComponent?> watcher)
    {
        if (!_actorQuery.Resolve(watcher, ref watcher.Comp2, false))
        {
            watcher.Comp1.Overrides.Clear();
            return;
        }

        foreach (var compOverride in watcher.Comp1.Overrides)
        {
            if (!TryGetEntity(compOverride, out var over))
                continue;

            _viewSubscriber.RemoveViewSubscriber(over.Value, watcher.Comp2.PlayerSession);
        }

        watcher.Comp1.Overrides.Clear();
        Dirty(watcher, watcher.Comp1);
    }
}
