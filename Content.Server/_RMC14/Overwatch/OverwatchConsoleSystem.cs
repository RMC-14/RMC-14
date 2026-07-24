using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Watch;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Overwatch;

public sealed class OverwatchConsoleSystem : SharedOverwatchConsoleSystem
{
    [Dependency] private readonly CommunicationsTowerSystem _communicationsTower = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

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
        foreach (var watching in new List<EntityUid>(ent.Comp.Watching))
        {
            if (TerminatingOrDeleted(watching))
                continue;

            if (TryComp(watching, out ActorComponent? actor) && actor != null)
                Unwatch(watching, actor.PlayerSession);
            else
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

        OverwatchWatchingComponent watching;
        if (TryComp(watcher.Owner, out OverwatchWatchingComponent? previous))
        {
            watching = previous;
            if (previous.Watching is { } previousTarget && previousTarget != toWatch.Owner)
            {
                _viewSubscriber.RemoveViewSubscriber(previousTarget, watcher.Comp1.PlayerSession);
                if (TryComp(previousTarget, out OverwatchCameraComponent? previousCamera))
                {
                    previousCamera.Watching.Remove(watcher);
                    Dirty(previousTarget, previousCamera);
                }
            }
        }
        else
        {
            watching = EnsureComp<OverwatchWatchingComponent>(watcher);
        }

        watching.Watching = toWatch;
        Dirty(watcher.Owner, watching);
        toWatch.Comp.Watching.Add(watcher);
        Dirty(toWatch.Owner, toWatch.Comp);
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

        if (watching.Watching is { } watchedEntity &&
            TryComp(watchedEntity, out OverwatchCameraComponent? watched))
        {
            watched.Watching.Remove(toRemove);
            Dirty(watchedEntity, watched);
        }

        watching.Watching = null;
        RemCompDeferred<OverwatchWatchingComponent>(toRemove);
    }
}
