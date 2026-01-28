using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Overwatch;
using Content.Shared.Ghost;
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

    // Camera radius in tiles while zoomed out, this is used to force-send entities within visible tiles to the watcher.
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
        var isCameraForced = false;
        if (watchSession != null && watchSession.AttachedEntity is not null)
        {
            try
            {
                _pvsOverride.AddForceSend(toWatch.Owner, watchSession);
                isCameraForced = true;
            }
            catch
            {
            }
        }

        RemoveWatcher(watcher);
        var watchingComp = EnsureComp<OverwatchWatchingComponent>(watcher);
        watchingComp.Watching = toWatch;
        watchingComp.isForceSent = isCameraForced;

        // Only force-sends nearby entities if extra zoom/offset is active, and the watcher and watched are on different maps
        var forced = new List<EntityUid>();
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
                            if (TryComp(ent, out GhostComponent? _))
                                continue;

                            _pvsOverride.AddForceSend(ent, watchSession);
                            forced.Add(ent);
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

        watchingComp.ForcedEntities = forced.Count > 0 ? forced : null;
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
            if (TryComp(watcher, out OverwatchWatchingComponent? watchingComp) && watchingComp.isForceSent)
            {
                try
                {
                    _pvsOverride.RemoveForceSend(oldTarget.Value, player);
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

        // Removes any entities that were force-sent to the watcher
        if (TryComp(toRemove, out ActorComponent? actor) && watched != null)
        {
            var session = actor?.PlayerSession;
            if (session != null)
            {
                try
                {
                    if (watching.isForceSent)
                        _pvsOverride.RemoveForceSend(watched.Owner, session);
                }
                catch
                {
                }

                if (watching.ForcedEntities != null)
                {
                    foreach (var ent in watching.ForcedEntities)
                    {
                        try
                        {
                            _pvsOverride.RemoveForceSend(ent, session);
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

            // Removes force-sent entities when the camera zoom/offset is reset
            if (!watcherOffsetActive)
            {
                if (watchingComp.ForcedEntities != null && session != null)
                {
                    foreach (var ent in watchingComp.ForcedEntities)
                    {
                        try
                        {
                            _pvsOverride.RemoveForceSend(ent, session);
                        }
                        catch
                        {
                        }
                    }

                    watchingComp.ForcedEntities = null;
                }

                continue;
            }

            // Only force-sends nearby entities when the watcher and watched are on different maps
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
                var forced = new List<EntityUid>();
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
                                if (TryComp(ent, out GhostComponent? _))
                                {
                                    continue;
                                }

                                _pvsOverride.AddForceSend(ent, session);
                                forced.Add(ent);
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

                watchingComp.ForcedEntities = forced.Count > 0 ? forced : null;
            }
            else
            {
                watchingComp.ForcedEntities = null;
            }
        }
    }
}
