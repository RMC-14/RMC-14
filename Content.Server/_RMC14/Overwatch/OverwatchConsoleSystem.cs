using System.Linq;
using System.Numerics;
﻿using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Watch;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Overwatch;

public sealed class OverwatchConsoleSystem : SharedOverwatchConsoleSystem
{
    [Dependency] private readonly CommunicationsTowerSystem _communicationsTower = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly float _cameraRadius = OverwatchWatchingComponent.CameraRadius;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);

        SubscribeLocalEvent<OverwatchCameraComponent, ComponentRemove>(OnWatchedRemove);
        SubscribeLocalEvent<OverwatchCameraComponent, EntityTerminatingEvent>(OnWatchedRemove);
        SubscribeLocalEvent<OverwatchWatchingComponent, ComponentRemove>(OnWatchingRemove);
        SubscribeLocalEvent<OverwatchWatchingComponent, EntityTerminatingEvent>(OnWatchingRemove);
    }

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        foreach (var session in Player.Sessions)
        {
            if (session.AttachedEntity is not { } ent)
                continue;

            TryComp(ent, out OverwatchWatchingComponent? overwatch);
            TryComp(ent, out XenoWatchingComponent? xenoWatch);

            if (overwatch == null && xenoWatch == null)
                continue;

            var watched = overwatch?.Watching ?? xenoWatch?.Watching;

            if (watched is not { } target)
                continue;

            var targetCoordinates = TransformSystem.GetMoverCoordinates(target);
            var targetMap = TransformSystem.GetMap(targetCoordinates);
            if (overwatch != null && HasComp<RMCPlanetComponent>(targetMap) && targetMap != TransformSystem.GetMap(ent))
            {
                if (!_communicationsTower.CanTransmit())
                    continue;
            }

            if (!targetCoordinates.TryDistance(EntityManager, Transform(ev.Source).Coordinates, out var distance))
                continue;

            if (distance > ev.VoiceRange)
                continue;

            if (!ev.Recipients.ContainsKey(session))
                ev.Recipients.Add(session, new ChatSystem.ICChatRecipientData(distance, false, true));
        }
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
        watchingComp.IsOverridden = isCameraOverridden;

        var overridden = new List<EntityUid>();

        try
        {
            var mapCoords = _transform.GetMapCoordinates(toWatch.Owner);
            var nearby = _lookup.GetEntitiesInRange(mapCoords, _cameraRadius);
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
            if (TryComp(watcher, out OverwatchWatchingComponent? watchingComp) && watchingComp.IsOverridden)
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
                    if (watching.IsOverridden)
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

            var overridden = new List<EntityUid>();
            try
            {
                var mapCoords = _transform.GetMapCoordinates(watched);
                var nearby = _lookup.GetEntitiesInRange(mapCoords, _cameraRadius);
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
    }
}
