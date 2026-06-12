using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

public sealed class LarvaQueueSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly FollowerSystem _follower = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [ViewVariables]
    public static readonly Dictionary<EntityUid, HashSet<NetUserId>> InfectorSet = new();

    [ViewVariables]
    public static readonly Dictionary<EntityUid, LinkedList<NetUserId>> Queue = new();

    [ViewVariables]
    public static readonly Dictionary<EntityUid, Dictionary<NetUserId, double>> PreQueue = new();

    private readonly Dictionary<NetUserId, PendingOffer> _pendingOffers = new();
    private readonly HashSet<EntityUid> _reservedBurstLarva = new();
    private readonly Dictionary<EntityUid, int> _pendingBurrowedCount = new();
    private readonly Dictionary<NetUserId, TimeSpan> _disconnectedAt = new();

    private sealed class PendingOffer
    {
        public required EntityUid? TargetLarva;
        public required Entity<HiveComponent> Hive;
        public required double ExpiresAt;
    }

    private int _offerTimeoutSeconds;
    private TimeSpan _disconnectGracePeriod;

    public override void Initialize()
    {
        SubscribeLocalEvent<JoinXenoComponent, JoinLarvaQueueEvent>(OnJoinLarvaQueue);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<BurrowedLarvaAddedEvent>(OnBurrowedLarvaAdded);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CanBeLarvaQueuedComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<CanBeLarvaQueuedComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<XenoBurstPriorityEvent>(OnXenoBurstPriority);

        if (!_net.IsClient)
        {
            SubscribeNetworkEvent<LarvaQueueAcceptOfferEvent>(OnAcceptOffer);
            SubscribeNetworkEvent<LarvaQueueDeclineOfferEvent>(OnDeclineOffer);
            SubscribeNetworkEvent<LarvaQueueFollowTargetEvent>(OnFollowTarget);
        }

        Subs.CVar(_config, RMCCVars.RMCLarvaQueueOfferTimeoutSeconds, v => _offerTimeoutSeconds = v, true);
        Subs.CVar(_config, RMCCVars.RMCDisconnectedXenoGhostRoleTimeSeconds, v => _disconnectGracePeriod = TimeSpan.FromSeconds(v), true);
    }

    private void OnXenoBurstPriority(ref XenoBurstPriorityEvent ev)
    {
        if (_net.IsClient || ev.Hive == null)
            return;

        if (GetEntity(ev.Hive.Value) is not { Valid: true } hiveEntity
            || !TryComp(hiveEntity, out HiveComponent? hiveComp))
            return;

        var hive = new Entity<HiveComponent>(hiveEntity, hiveComp);

        if (ev.BurstVictimUserId.HasValue)
        {
            var victimId = ev.BurstVictimUserId.Value;
            var larvaEntity = ev.SpawnedLarva.HasValue ? GetEntity(ev.SpawnedLarva.Value) : EntityUid.Invalid;

            if (larvaEntity.IsValid()
                && TryComp<BursterComponent>(larvaEntity, out var burster)
                && _player.TryGetSessionByEntity(burster.BurstFrom, out var victimSession)
                && victimSession.UserId == victimId)
            {
                if (InfectorSet.TryGetValue(hiveEntity, out var iq)) iq.Remove(victimId);
                if (Queue.TryGetValue(hiveEntity, out var nq)) nq.Remove(victimId);
                if (PreQueue.TryGetValue(hiveEntity, out var pq)) pq.Remove(victimId);
                _reservedBurstLarva.Add(larvaEntity);
                SendOffer(victimSession, larvaEntity, hive, "Burst Victim", 1);
            }
        }

        if (ev.InfectorUserId.HasValue)
        {
            var infectorId = ev.InfectorUserId.Value;
            var inInfector = InfectorSet.TryGetValue(hiveEntity, out var iq2) && iq2.Contains(infectorId);

            if (!inInfector)
            {
                var removedFromQueue = Queue.TryGetValue(hiveEntity, out var nq) && nq.Remove(infectorId);
                var removedFromPreQueue = PreQueue.TryGetValue(hiveEntity, out var pq) && pq.Remove(infectorId);

                if (removedFromQueue || removedFromPreQueue)
                    InfectorSet.GetOrNew(hive).Add(infectorId);
            }
        }

        TryOfferToQueue(hiveEntity);
    }

    private void OnMindRemoved(Entity<CanBeLarvaQueuedComponent> ent, ref MindRemovedMessage _)
    {
        if (_net.IsClient || !HasComp<XenoComponent>(ent))
            return;

        if (_mobState.IsDead(ent))
            return;

        EnsureComp<LarvaQueuedComponent>(ent);
        if (_hive.GetHive(ent.Owner) is { } hive)
            TryOfferToQueue(hive.Owner);
    }

    private void OnMindAdded(Entity<CanBeLarvaQueuedComponent> ent, ref MindAddedMessage _)
    {
        if (_net.IsClient)
            return;

        RemCompDeferred<LarvaQueuedComponent>(ent);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (_net.IsClient)
            return;

        _disconnectedAt.Remove(ev.Player.UserId);

        var isQueueEligible = HasComp<GhostComponent>(ev.Entity)
            || TryComp<MindTakeoverBehaviorComponent>(ev.Entity, out var takeoverBehavior) && !takeoverBehavior.EjectFromLarvaQueues;

        if (isQueueEligible)
        {
            var hives = EntityQueryEnumerator<HiveComponent>();
            while (hives.MoveNext(out var hiveId, out _))
            {
                if (InfectorSet.TryGetValue(hiveId, out var iqSet) && iqSet.Contains(ev.Player.UserId)
                    || Queue.TryGetValue(hiveId, out var nq) && nq.Contains(ev.Player.UserId))
                {
                    TryOfferToQueue(hiveId);
                }
            }
            return;
        }

        RemoveFromAllQueues(ev.Player.UserId);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        InfectorSet.Clear();
        Queue.Clear();
        PreQueue.Clear();
        _pendingOffers.Clear();
        _reservedBurstLarva.Clear();
        _pendingBurrowedCount.Clear();
        _disconnectedAt.Clear();
    }

    private void OnJoinLarvaQueue(Entity<JoinXenoComponent> ent, ref JoinLarvaQueueEvent args)
    {
        if (_net.IsClient)
            return;

        var denyQueuing = _config.GetCVar(RMCCVars.RMCLarvaQueueRoundstartDelaySeconds);
        if (_gameTicker.RoundDuration().TotalSeconds <= denyQueuing)
            return;

        if (!TryComp(ent, out ActorComponent? actor) || !TryComp(ent, out GhostComponent? ghost) ||
            !TryGetEntity(args.Hive, out var hiveUid) || !TryComp(hiveUid, out HiveComponent? hiveComp))
            return;

        var hive = new Entity<HiveComponent>(hiveUid.Value, hiveComp);
        var session = actor.PlayerSession.UserId;
        var actorEntity = actor.PlayerSession.AttachedEntity;
        var larvaWaitTime = _config.GetCVar(RMCCVars.RMCLarvaQueueWaitSeconds);

        if (actorEntity == null)
            return;

        if (TryRemoveFromQueue(session, hive))
        {
            _popup.PopupEntity("You have been removed from the queue.", actorEntity.Value, actorEntity.Value);
            return;
        }

        if ((_gameTiming.CurTime - ghost.TimeOfDeath).TotalSeconds >= larvaWaitTime)
        {
            Queue.GetOrNew(hive).AddLast(session);
            var position = GetQueuePosition(session, hive);
            _popup.PopupEntity(
                $"You have been added to the queue at position {position}.",
                actorEntity.Value, actorEntity.Value);

            TryOfferToQueue(hiveUid.Value);
        }
        else
        {
            PreQueue.GetOrNew(hive).Add(session, larvaWaitTime + ghost.TimeOfDeath.TotalSeconds);
            var timeLeft = TimeSpan.FromSeconds(larvaWaitTime) + ghost.TimeOfDeath - _gameTiming.CurTime;
            _popup.PopupEntity(
                $"You died too recently, and will be added to the queue in {timeLeft.TotalSeconds:F0} seconds.",
                actorEntity.Value, actorEntity.Value);
        }
    }

    private void OnBurrowedLarvaAdded(BurrowedLarvaAddedEvent ev)
    {
        TryOfferToQueue(ev.Hive);
    }

    private void OnAcceptOffer(LarvaQueueAcceptOfferEvent _, EntitySessionEventArgs args)
    {
        if (!_pendingOffers.TryGetValue(args.SenderSession.UserId, out var offer))
            return;

        _pendingOffers.Remove(args.SenderSession.UserId);

        if (offer.TargetLarva is { } larvaEnt)
        {
            _reservedBurstLarva.Remove(larvaEnt);

            if (!EntityManager.EntityExists(larvaEnt) || _mobState.IsDead(larvaEnt))
            {
                SendOfferExpired(args.SenderSession);
                return;
            }

            var mindId = _mind.TryGetMind(args.SenderSession.UserId, out var existingMindId, out var existingMindComp)
                ? existingMindId!.Value
                : (EntityUid) _mind.CreateMind(args.SenderSession.UserId, Name(larvaEnt));
            _mind.TransferTo(mindId, larvaEnt, ghostCheckOverride: true);
            RemCompDeferred<LarvaQueuedComponent>(larvaEnt);
        }
        else
        {
            DecrementPendingBurrowed(offer.Hive);
            _hive.JoinBurrowedLarva(offer.Hive, args.SenderSession);
        }
    }

    private void OnDeclineOffer(LarvaQueueDeclineOfferEvent _, EntitySessionEventArgs args)
    {
        ExpireOffer(args.SenderSession.UserId, notifyPlayer: false);
    }

    private void OnFollowTarget(LarvaQueueFollowTargetEvent _, EntitySessionEventArgs args)
    {
        if (!_pendingOffers.TryGetValue(args.SenderSession.UserId, out var offer)
            || offer.TargetLarva == null)
            return;

        var ghost = args.SenderSession.AttachedEntity;
        if (ghost == null || !HasComp<GhostComponent>(ghost.Value))
            return;

        if (EntityManager.EntityExists(offer.TargetLarva.Value))
            _follower.StartFollowingEntity(ghost.Value, offer.TargetLarva.Value);
    }

    private void TryOfferToQueue(NetEntity hiveNet)
    {
        if (GetEntity(hiveNet) is not { Valid: true } hiveEntity)
            return;

        TryOfferToQueue(hiveEntity);
    }

    private void TryOfferToQueue(EntityUid hiveId)
    {
        if (!TryComp(hiveId, out HiveComponent? hiveComp))
            return;

        if (_net.IsClient)
            return;

        var hive = new Entity<HiveComponent>(hiveId, hiveComp);
        var originalNormalCount = Queue.TryGetValue(hiveId, out var nqBefore) ? nqBefore.Count : 0;

        TryOfferQueuedEntities(hive);
        TryOfferBurrowedLarva(hive);

        var newCount = Queue.TryGetValue(hiveId, out var nqAfter) ? nqAfter.Count : 0;
        if (newCount >= originalNormalCount || nqAfter == null)
            return;

        var position = 1;
        foreach (var userId in nqAfter)
        {
            if (_player.TryGetSessionById(userId, out var session) && session.AttachedEntity != null)
            {
                _popup.PopupEntity(
                    $"You are now in position {position} of the larva queue for ({Name(hiveId)}).",
                    session.AttachedEntity.Value,
                    session.AttachedEntity.Value);
            }
            ++position;
        }
    }

    private void TryOfferQueuedEntities(Entity<HiveComponent> hive)
    {
        if (!HasAnyQueued(hive.Owner))
            return;

        var larva = EntityQueryEnumerator<LarvaQueuedComponent>();
        while (larva.MoveNext(out var ent, out _))
        {
            if (_mobState.IsDead(ent))
            {
                RemCompDeferred<LarvaQueuedComponent>(ent);
                continue;
            }

            if (!_hive.IsMember(ent, hive))
                continue;

            if (_reservedBurstLarva.Contains(ent))
                continue;

            if (!HasAnyQueued(hive.Owner))
                break;

            if (TryPopNextSessionForOffer(hive, out var session, out var tier, out var pos))
            {
                _reservedBurstLarva.Add(ent);
                SendOffer(session, ent, hive, tier, pos);
            }
            else
            {
                break; // no more players in queue
            }
        }
    }

    private void TryOfferBurrowedLarva(Entity<HiveComponent> hive)
    {
        if (!HasAnyQueued(hive.Owner))
            return;

        var available = hive.Comp.BurrowedLarva
            - _pendingBurrowedCount.GetValueOrDefault(hive, 0);

        for (var i = 0; i < available; i++)
        {
            if (!HasAnyQueued(hive.Owner))
                break;

            if (TryPopNextSessionForOffer(hive, out var session, out var tier, out var pos))
            {
                _pendingBurrowedCount[hive] = _pendingBurrowedCount.GetValueOrDefault(hive, 0) + 1;
                SendOffer(session, null, hive, tier, pos);
            }
            else
            {
                break; // no more players in queue
            }
        }
    }

    private void SendOffer(ICommonSession session, EntityUid? targetLarva, Entity<HiveComponent> hive, string tier, int position)
    {
        if (_pendingOffers.TryGetValue(session.UserId, out var existing))
        {
            if (existing.TargetLarva is { } existingLarva)
                _reservedBurstLarva.Remove(existingLarva);
            else
                DecrementPendingBurrowed(existing.Hive);
        }

        var expiresAt = _gameTiming.CurTime.TotalSeconds + _offerTimeoutSeconds;
        _pendingOffers[session.UserId] = new PendingOffer
        {
            TargetLarva = targetLarva,
            Hive = hive,
            ExpiresAt = expiresAt,
        };

        RaiseNetworkEvent(new LarvaQueueOfferEvent
        {
            TargetEntity = targetLarva.HasValue ? GetNetEntity(targetLarva.Value) : null,
            ExpiresAt = expiresAt,
            HiveName = Name(hive),
            OfferType = tier,
            QueuePosition = position,
        }, session);
    }

    private void SendOfferExpired(ICommonSession session)
    {
        RaiseNetworkEvent(new LarvaQueueOfferExpiredEvent(), session);
    }

    private void ExpireOffer(NetUserId userId, bool notifyPlayer, bool requeue = false)
    {
        if (!_pendingOffers.TryGetValue(userId, out var offer))
            return;

        _pendingOffers.Remove(userId);

        if (offer.TargetLarva is { } larvaEnt)
            _reservedBurstLarva.Remove(larvaEnt);
        else
            DecrementPendingBurrowed(offer.Hive);

        if (requeue && !IsAlreadyQueued(userId, offer.Hive.Owner))
        {
            Queue.GetOrNew(offer.Hive).AddFirst(userId);
            if (_player.TryGetSessionById(userId, out var requeueSession) && requeueSession.AttachedEntity != null)
            {
                _popup.PopupEntity(
                    "The larva you were offered has died. You have been moved back to the front of the queue.",
                    requeueSession.AttachedEntity.Value,
                    requeueSession.AttachedEntity.Value);
            }
        }

        if (notifyPlayer && _player.TryGetSessionById(userId, out var session))
            SendOfferExpired(session);

        TryOfferToQueue(offer.Hive.Owner);
    }

    private void DecrementPendingBurrowed(Entity<HiveComponent> hive)
    {
        var current = _pendingBurrowedCount.GetValueOrDefault(hive, 0);
        if (current > 1)
            _pendingBurrowedCount[hive] = current - 1;
        else
            _pendingBurrowedCount.Remove(hive);
    }

    private bool HasAnyQueued(EntityUid hive)
    {
        if (InfectorSet.TryGetValue(hive, out var iqSet) && iqSet.Count > 0)
            return true;
        if (Queue.TryGetValue(hive, out var nq) && nq.Count > 0)
            return true;
        return false;
    }

    private bool TryPopNextSessionForOffer(
        Entity<HiveComponent> hive,
        [NotNullWhen(true)] out ICommonSession? nextSession,
        out string tier,
        out int position)
    {
        nextSession = null;
        tier = "Normal";
        position = 0;

        InfectorSet.TryGetValue(hive, out var iqSet);
        Queue.TryGetValue(hive, out var nq);

        if (iqSet != null && TryPopFromSet(iqSet, out nextSession))
        {
            tier = "Infector";
            position = 1;
            return true;
        }

        var iqCount = iqSet?.Count ?? 0;

        if (nq != null && TryPopFromList(nq, iqCount, out nextSession, out position))
        {
            tier = "Normal";
            return true;
        }

        return false;
    }

    private bool TryPopFromSet(
        HashSet<NetUserId> set,
        [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        NetUserId? found = null;
        List<NetUserId>? reembodied = null;

        foreach (var netId in set)
        {
            if (!_player.TryGetSessionById(netId, out var s) || s.AttachedEntity == null)
            {
                if (!_disconnectedAt.ContainsKey(netId))
                    _disconnectedAt[netId] = _gameTiming.CurTime + _disconnectGracePeriod;
                continue;
            }

            if (!HasComp<GhostComponent>(s.AttachedEntity.Value))
            {
                if (TryComp<MindTakeoverBehaviorComponent>(s.AttachedEntity.Value, out var takeover) && !takeover.EjectFromLarvaQueues)
                {
                    session = s;
                    found = netId;
                    break;
                }

                reembodied ??= new List<NetUserId>();
                reembodied.Add(netId);
                continue;
            }

            session = s;
            found = netId;
            break;
        }

        if (found.HasValue)
            set.Remove(found.Value);

        if (reembodied != null)
            foreach (var id in reembodied)
                set.Remove(id);

        return session != null;
    }

    private bool TryPopFromList(
        LinkedList<NetUserId> list,
        int offset,
        [NotNullWhen(true)] out ICommonSession? session,
        out int absolutePosition)
    {
        session = null;
        absolutePosition = 0;
        var indexInList = 0;

        for (var node = list.First; node != null;)
        {
            var next = node.Next;
            var netId = node.Value;
            indexInList++;

            if (!_player.TryGetSessionById(netId, out var s) || s.AttachedEntity == null)
            {
                if (!_disconnectedAt.ContainsKey(netId))
                    _disconnectedAt[netId] = _gameTiming.CurTime + _disconnectGracePeriod;
                node = next;
                continue;
            }

            if (!HasComp<GhostComponent>(s.AttachedEntity.Value))
            {
                if (TryComp<MindTakeoverBehaviorComponent>(s.AttachedEntity.Value, out var takeover) && !takeover.EjectFromLarvaQueues)
                {
                    absolutePosition = offset + indexInList;
                    list.Remove(node);
                    session = s;
                    return true;
                }

                list.Remove(node);
                node = next;
                continue;
            }

            absolutePosition = offset + indexInList;
            list.Remove(node);
            session = s;
            return true;
        }

        return false;
    }

    /// <summary>
    /// This will try and remove them from a queue and return true if they are part of that queue.
    /// </summary>
    /// <param name="netUserId">ID of the person we are removing</param>
    /// <param name="hive">The hive that we are removing them from the queue of</param>
    /// <returns></returns>
    private bool TryRemoveFromQueue(NetUserId netUserId, Entity<HiveComponent> hive)
    {
        if (PreQueue.TryGetValue(hive, out var pq) && pq.Remove(netUserId))
            return true;
        if (InfectorSet.TryGetValue(hive, out var iqSet) && iqSet.Remove(netUserId))
            return true;
        if (Queue.TryGetValue(hive, out var nq) && nq.Remove(netUserId))
            return true;
        return false;
    }

    private void RemoveFromAllQueues(NetUserId netUserId)
    {
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            TryRemoveFromQueue(netUserId, (hiveId, hive));
        }
    }

    public bool IsAlreadyQueued(NetUserId userId, EntityUid hiveId)
    {
        if (PreQueue.TryGetValue(hiveId, out var pq) && pq.ContainsKey(userId))
            return true;
        if (InfectorSet.TryGetValue(hiveId, out var iqSet) && iqSet.Contains(userId))
            return true;
        if (Queue.TryGetValue(hiveId, out var nq) && nq.Contains(userId))
            return true;
        return false;
    }

    public int GetQueuePosition(NetUserId userId, EntityUid hiveId)
    {
        InfectorSet.TryGetValue(hiveId, out var iqSet);

        if (iqSet != null && iqSet.Contains(userId))
            return 1;

        var iqCount = iqSet?.Count ?? 0;

        if (Queue.TryGetValue(hiveId, out var nq))
        {
            var pos = iqCount + 1;
            foreach (var id in nq)
            {
                if (id == userId) return pos;
                pos++;
            }
        }

        return -1;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _gameTiming.CurTime;

        var expiredDisconnected = new List<NetUserId>();
        foreach (var (userId, expiry) in _disconnectedAt)
        {
            if (time >= expiry)
                expiredDisconnected.Add(userId);
        }

        foreach (var userId in expiredDisconnected)
        {
            _disconnectedAt.Remove(userId);
            RemoveFromAllQueues(userId);
        }

        foreach (var (hive, hivePreQueue) in PreQueue)
        {
            if (!TryComp(hive, out HiveComponent? hiveComp))
                continue;

            var toPromote = new List<NetUserId>();
            foreach (var (userId, userTime) in hivePreQueue)
            {
                if (userTime <= time.TotalSeconds)
                    toPromote.Add(userId);
            }

            foreach (var userId in toPromote)
            {
                hivePreQueue.Remove(userId);
                Queue.GetOrNew(hive).AddLast(userId);

                if (_player.TryGetSessionById(userId, out var session) && session.AttachedEntity != null)
                {
                    var pos = GetQueuePosition(userId, hive);
                    _popup.PopupEntity(
                        $"You have been added to the queue at position {pos}.",
                        session.AttachedEntity.Value, session.AttachedEntity.Value);
                }
            }

            if (toPromote.Count > 0)
                TryOfferToQueue(hive);
        }

        var expiredOffers = new List<NetUserId>();
        var entityDiedOffers = new List<NetUserId>();
        foreach (var (userId, offer) in _pendingOffers)
        {
            if (time.TotalSeconds >= offer.ExpiresAt)
            {
                expiredOffers.Add(userId);
                continue;
            }

            if (offer.TargetLarva is { } larvaEnt
                && (!EntityManager.EntityExists(larvaEnt) || _mobState.IsDead(larvaEnt)))
            {
                entityDiedOffers.Add(userId);
            }
        }

        foreach (var userId in expiredOffers)
            ExpireOffer(userId, notifyPlayer: true, requeue: false);

        foreach (var userId in entityDiedOffers)
            ExpireOffer(userId, notifyPlayer: true, requeue: true);
    }
}
