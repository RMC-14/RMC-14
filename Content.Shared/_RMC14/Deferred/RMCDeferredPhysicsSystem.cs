using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Deferred;

// TODO RMC14 See if this can be removed after predicted collision events are buffered in RobustToolbox.
/// <summary>
/// Defers physics operations that can invalidate contacts until after collision events finish being raised.
/// </summary>
public sealed partial class RMCDeferredPhysicsSystem : EntitySystem
{
    [Dependency] private SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private ThrownItemSystem _thrownItem = default!;

    private readonly HashSet<EntityUid> _pendingContactRegenerations = new();
    private readonly HashSet<EntityUid> _pendingThrowStops = new();

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ThrownItemComponent> _thrownItemQuery;

    public override void Initialize()
    {
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _thrownItemQuery = GetEntityQuery<ThrownItemComponent>();

        SubscribeLocalEvent<DeferredRegenerateContactsEvent>(OnDeferredRegenerateContacts);
        SubscribeLocalEvent<DeferredStopThrowEvent>(OnDeferredStopThrow);
    }

    private void OnDeferredRegenerateContacts(DeferredRegenerateContactsEvent args)
    {
        try
        {
            if (_physicsQuery.TryGetComponent(args.Uid, out var physics) && physics.Awake)
                _broadphase.RegenerateContacts((args.Uid, physics));
        }
        finally
        {
            _pendingContactRegenerations.Remove(args.Uid);
        }
    }

    private void OnDeferredStopThrow(DeferredStopThrowEvent args)
    {
        try
        {
            if (!_thrownItemQuery.TryGetComponent(args.Uid, out var thrown))
                return;

            if (args.Land)
            {
                if (!_physicsQuery.TryGetComponent(args.Uid, out var physics))
                    return;

                _thrownItem.LandComponent(args.Uid, thrown, physics, args.PlayLandSound);
            }

            _thrownItem.StopThrow(args.Uid, thrown);
        }
        finally
        {
            _pendingThrowStops.Remove(args.Uid);
        }
    }

    /// <summary>
    /// Queues landing and stopping a thrown entity.
    /// </summary>
    /// <returns>Returns True if queued, False if it is already queued or no longer being thrown.</returns>
    public bool TryQueueLandAndStopThrow(EntityUid uid, bool playLandSound = true)
    {
        if (!_physicsQuery.HasComp(uid) || !_thrownItemQuery.HasComp(uid) || !_pendingThrowStops.Add(uid))
            return false;

        QueueLocalEvent(new DeferredStopThrowEvent(uid, true, playLandSound));
        return true;
    }

    /// <summary>
    /// Queues stopping a thrown entity.
    /// </summary>
    /// <returns>Returns True if queued, False if it is already queued or no longer being thrown.</returns>
    public bool TryQueueStopThrow(EntityUid uid)
    {
        if (!_thrownItemQuery.HasComp(uid) || !_pendingThrowStops.Add(uid))
            return false;

        QueueLocalEvent(new DeferredStopThrowEvent(uid, false, false));
        return true;
    }

    /// <summary>
    /// Queues contact regeneration for an awake physics body.
    /// </summary>
    /// <returns>Returns True if queued, False if the entity has no physics body or already has contact regeneration queued.</returns>
    public bool TryQueueRegenerateContacts(EntityUid uid)
    {
        if (!_physicsQuery.HasComp(uid) || !_pendingContactRegenerations.Add(uid))
            return false;

        QueueLocalEvent(new DeferredRegenerateContactsEvent(uid));
        return true;
    }

    private sealed class DeferredRegenerateContactsEvent(EntityUid uid) : EntityEventArgs
    {
        public readonly EntityUid Uid = uid;
    }

    private sealed class DeferredStopThrowEvent(EntityUid uid, bool land, bool playLandSound) : EntityEventArgs
    {
        public readonly EntityUid Uid = uid;
        public readonly bool Land = land;
        public readonly bool PlayLandSound = playLandSound;
    }
}
