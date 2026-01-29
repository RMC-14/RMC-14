using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Overwatch;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Overwatch;

public sealed class OverwatchConsoleSystem : SharedOverwatchConsoleSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    // Camera radius while zoomed out, this is used to load entities within visible tiles to the watcher.
    // 28f radius is almost always enough with zoom: 1.5f and offset: 10f
    private const float cameraRadius = 28f;

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

            RemoveWatcher(watching);
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

        var watchSession = watcher.Comp1.PlayerSession;
        var isCameraOverridden = false;
        if (watchSession != null && watchSession.AttachedEntity is not null)
        {
            try
            {
                _pvsOverride.AddSessionOverride(toWatch.Owner, watchSession);
                isCameraOverridden = true;
            }
            catch
            {
            }
        }

        RemoveWatcher(watcher);
        var watchingComp = EnsureComp<OverwatchWatchingComponent>(watcher);
        watchingComp.Watching = toWatch;
        watchingComp.isOverridden = isCameraOverridden;

        // Only overrides nearby entities if extra zoom/offset is active, and the watcher and watched are on different maps
        var overridden = new List<EntityUid>();
        var offsetActive = watcher.Comp2 != null && watcher.Comp2.Offset != Vector2.Zero;
        var watchingCrossMap = false;
        try
        {
            var watchMap = Transform(toWatch.Owner).MapUid;
            if (watchMap != null && watcher.Comp1.PlayerSession.AttachedEntity is { } attached && Transform(attached).MapUid != watchMap)
                watchingCrossMap = true;
        }
        catch
        {
            
        }

        if (offsetActive && watchingCrossMap)
        {
            try
            {
                var mapCoords = _transform.GetMapCoordinates(toWatch.Owner);
                var nearby = _lookup.GetEntitiesInRange(mapCoords, cameraRadius);
                foreach (var ent in nearby)
                {
                    if (ent == watcher.Owner)
                        continue;

                    try
                    {
                        if (watchSession != null)
                        {
                            _pvsOverride.AddSessionOverride(ent, watchSession);
                            overridden.Add(ent);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                
            }
        }

        watchingComp.OverriddenEntities = overridden.Count > 0 ? overridden : null;
        toWatch.Comp.Watching.Add(watcher);
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        var oldTarget = watcher.Comp.Target;

        base.Unwatch(watcher, player);

        if (oldTarget != null && oldTarget != watcher.Owner)
        {
            _viewSubscriber.RemoveViewSubscriber(oldTarget.Value, player);
            if (TryComp(watcher, out OverwatchWatchingComponent? watchingComp) && watchingComp.isOverridden)
            {
                try
                {
                    _pvsOverride.RemoveSessionOverride(oldTarget.Value, player);
                }
                catch
                {
                }
            }
        }

        RemoveWatcher(watcher);
    }

    private void RemoveWatcher(EntityUid toRemove)
    {
        if (!TryComp(toRemove, out OverwatchWatchingComponent? watching))
            return;

        if (TryComp(watching.Watching, out OverwatchCameraComponent? watched))
            watched.Watching.Remove(toRemove);

        // Removes any entities that were overridden to load for the watcher
        if (TryComp(toRemove, out ActorComponent? actor) && watched != null)
        {
            var session = actor?.PlayerSession;
            if (session != null)
            {
                try
                {
                    if (watching.isOverridden)
                        _pvsOverride.RemoveSessionOverride(watched.Owner, session);
                }
                catch
                {
                }

                if (watching.OverriddenEntities != null)
                {
                    foreach (var ent in watching.OverriddenEntities)
                    {
                        try
                        {
                            _pvsOverride.RemoveSessionOverride(ent, session);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        watching.Watching = null;
        RemCompDeferred<OverwatchWatchingComponent>(toRemove);
    }

    protected override void OnCameraAdjustOffsetEvent(OverwatchCameraAdjustOffsetEvent args)
    {
        base.OnCameraAdjustOffsetEvent(args);
        
        if (!TryGetEntity(args.Actor, out var watcherEntity) ||
            !TryComp(watcherEntity, out OverwatchWatchingComponent? watcherComp) || 
            watcherComp.Watching == null)
            return;

        var watched = watcherComp.Watching.Value;
        if (!TryComp(watched, out OverwatchCameraComponent? camComp))
            return;

        foreach (var watcher in camComp.Watching.ToArray())
        {
            if (TerminatingOrDeleted(watcher) ||
                !TryComp(watcher, out OverwatchWatchingComponent? watchingComp) ||
                !TryComp(watcher, out ActorComponent? actor))
                continue;

            var session = actor.PlayerSession;
            var watcherOffsetActive = false;
            try
            {
                if (TryComp(watcher, out EyeComponent? watcherEye))
                    watcherOffsetActive = watcherEye.Offset != Vector2.Zero;
            }
            catch
            {
            }

            // Removes overridden entities when the camera zoom/offset is reset
            if (!watcherOffsetActive)
            {
                if (watchingComp.OverriddenEntities != null && session != null)
                {
                    foreach (var ent in watchingComp.OverriddenEntities)
                    {
                        try
                        {
                            _pvsOverride.RemoveSessionOverride(ent, session);
                        }
                        catch
                        {
                        }
                    }

                    watchingComp.OverriddenEntities = null;
                }

                continue;
            }

            // Only overrides nearby entities when the watcher and watched are on different maps
            var watchingCrossMap = false;
            try
            {
                var watchMap = Transform(watched).MapUid;
                if (watchMap != null && actor.PlayerSession.AttachedEntity is { } attached && Transform(attached).MapUid != watchMap)
                    watchingCrossMap = true;
            }
            catch
            {
                
            }

            if (watchingCrossMap)
            {
                var overridden = new List<EntityUid>();
                try
                {
                    var mapCoords = _transform.GetMapCoordinates(watched);
                    var nearby = _lookup.GetEntitiesInRange(mapCoords, cameraRadius);
                    foreach (var ent in nearby)
                    {
                        if (ent == watcher)
                            continue;

                        try
                        {
                            if (session != null)
                            {
                                _pvsOverride.AddSessionOverride(ent, session);
                                overridden.Add(ent);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                watchingComp.OverriddenEntities = overridden.Count > 0 ? overridden : null;
            }
            else
            {
                watchingComp.OverriddenEntities = null;
            }
        }
    }
}
