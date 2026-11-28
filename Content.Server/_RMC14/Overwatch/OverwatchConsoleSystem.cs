using System.Numerics;
using Content.Server.Chat.Systems;
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
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly HashSet<EntityUid> _nearby = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);

        SubscribeLocalEvent<OverwatchCameraComponent, ComponentRemove>(OnWatchedRemove);
        SubscribeLocalEvent<OverwatchCameraComponent, EntityTerminatingEvent>(OnWatchedRemove);
        SubscribeLocalEvent<OverwatchCameraComponent, RMCOverwatchTripodSquadChangedEvent>(OnWatchedSquadChanged);
        SubscribeLocalEvent<OverwatchWatchingComponent, ComponentRemove>(OnWatchingRemove);
        SubscribeLocalEvent<OverwatchWatchingComponent, EntityTerminatingEvent>(OnWatchingRemove);
        SubscribeLocalEvent<OverwatchWatchingComponent, PlayerDetachedEvent>(OnPlayerDetached);
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
        RemoveWatchers(ent);
    }

    private void OnWatchedSquadChanged(
        Entity<OverwatchCameraComponent> ent,
        ref RMCOverwatchTripodSquadChangedEvent args)
    {
        RemoveWatchers(ent);
        ent.Comp.Watching.Clear();
        Dirty(ent);
    }

    private void RemoveWatchers(Entity<OverwatchCameraComponent> ent)
    {
        foreach (var watching in new List<EntityUid>(ent.Comp.Watching))
        {
            if (TerminatingOrDeleted(watching))
                continue;

            RemoveWatcher(watching);
        }
    }

    private void OnWatchingRemove<T>(Entity<OverwatchWatchingComponent> ent, ref T args)
    {
        RemoveWatcher(ent, removeComponent: false);
    }

    private void OnPlayerDetached(Entity<OverwatchWatchingComponent> ent, ref PlayerDetachedEvent args)
    {
        RemoveWatcher(ent);
    }

    protected override void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<OverwatchCameraComponent?> toWatch)
    {
        if (watcher.Owner == toWatch.Owner ||
            !Resolve(watcher, ref watcher.Comp1, ref watcher.Comp2) ||
            !Resolve(toWatch, ref toWatch.Comp, false) ||
            TerminatingOrDeleted(toWatch))
        {
            return;
        }

        var session = watcher.Comp1.PlayerSession;
        RemoveWatcher(watcher, removeComponent: false);
        _eye.SetTarget(watcher, toWatch, watcher);

        var watching = EnsureComp<OverwatchWatchingComponent>(watcher);
        watching.Watching = toWatch;
        watching.WatchingSession = session;

        _nearby.Clear();
        _lookup.GetEntitiesInRange(toWatch.Owner, OverwatchWatchingComponent.CameraRadius, _nearby);
        _nearby.Add(toWatch);
        watching.OverriddenEntities ??= new();
        foreach (var nearby in _nearby)
        {
            if (nearby == watcher.Owner || TerminatingOrDeleted(nearby))
                continue;

            _pvsOverride.AddSessionOverride(nearby, session);
            watching.OverriddenEntities.Add(nearby);
        }

        toWatch.Comp.Watching.Add(watcher);
        Dirty(toWatch.Owner, toWatch.Comp);
        Dirty(watcher.Owner, watching);
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        base.Unwatch(watcher, player);
        RemoveWatcher(watcher);
    }

    private void RemoveWatcher(EntityUid actor, bool removeComponent = true)
    {
        if (!TryComp(actor, out OverwatchWatchingComponent? watching) ||
            watching.Watching is not { } target)
        {
            return;
        }

        watching.Watching = null;
        if (TryComp(target, out OverwatchCameraComponent? camera) && camera.Watching.Remove(actor))
            Dirty(target, camera);

        if (TryComp(actor, out EyeComponent? eye) && eye.Target == target)
        {
            _eye.SetOffset(actor, Vector2.Zero, eye);
            _eye.SetTarget(actor, null, eye);
        }

        if (watching.WatchingSession is { } session)
        {
            _viewSubscriber.RemoveViewSubscriber(target, session);
            if (watching.OverriddenEntities is { } overridden)
            {
                foreach (var uid in overridden)
                {
                    _pvsOverride.RemoveSessionOverride(uid, session);
                }

                overridden.Clear();
            }

            watching.WatchingSession = null;
        }

        if (removeComponent)
            RemComp<OverwatchWatchingComponent>(actor);
    }
}
