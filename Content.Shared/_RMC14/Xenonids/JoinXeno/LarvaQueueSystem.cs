using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Hive;
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
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [ViewVariables]
    public static readonly Dictionary<EntityUid, LinkedList<NetUserId>> Queue = new();

    [ViewVariables]
    public static readonly Dictionary<EntityUid, Dictionary<NetUserId, double>> PreQueue = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<JoinXenoComponent, JoinLarvaQueueEvent>(OnJoinLarvaQueue);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<BurrowedLarvaAddedEvent>(OnBurrowedLarvaAdded);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CanBeLarvaQueuedComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindRemoved(Entity<CanBeLarvaQueuedComponent> ent, ref MindRemovedMessage args)
    {
        if (!HasComp<XenoComponent>(ent))
            return;

        EnsureComp<LarvaQueuedComponent>(ent);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (_net.IsClient)
            return;

        //Ignore people moving around ghost bodies.
        if (HasComp<GhostComponent>(ev.Entity))
            return;

        //Don't eject from queue if entity is configured not to.
        if (TryComp<MindTakeoverBehaviorComponent>(ev.Entity, out var takeoverBehavior)
            && !takeoverBehavior.EjectFromLarvaQueues)
            return;

        RemoveFromAllQueues(ev.Player.UserId);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        Queue.Clear();
        PreQueue.Clear();
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

        if (TryRemoveFromQueue(actor.PlayerSession.UserId, hive))
        {
            _popup.PopupEntity($"You have been removed from the queue", actorEntity.Value, actorEntity.Value);
            return;
        }

        if ((_gameTiming.CurTime - ghost.TimeOfDeath).TotalSeconds >= larvaWaitTime)
        {
            Queue.GetOrNew(hive).AddLast(session);
            _popup.PopupEntity($"You have been added to the queue at position {Queue.GetOrNew(hive).Count}",
                actorEntity.Value, actorEntity.Value);
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
        SpawnLarva(ev.Hive);
    }

    private void SpawnLarva(NetEntity hiveNet)
    {
        if (GetEntity(hiveNet) is not { Valid: true } hiveEntity)
            return;

        SpawnLarva(hiveEntity);
    }

    private void SpawnLarva(Entity<HiveComponent?> hive)
    {
        if (!Resolve(hive, ref hive.Comp))
            return;

        var list = Queue.GetOrNew(hive);
        var originalLength = list.Count;
        if (list.Count <= 0)
            return;

        BurstLarvaSpawn((hive, hive.Comp));
        BurrowedLarvaSpawn((hive, hive.Comp));

        //Popup to people in queue if the queue length changed.
        if (list.Count >= originalLength)
            return;

        var position = 1;
        foreach (var userId in list)
        {
            if (_player.TryGetSessionById(userId, out var session) && session.AttachedEntity != null)
            {
                _popup.PopupEntity($"You are now in position {position} of the larva queue for ({Name(hive)}).",
                    session.AttachedEntity.Value,
                    session.AttachedEntity.Value);
            }
            ++position;
        }
    }

    private void BurstLarvaSpawn(Entity<HiveComponent> hive)
    {
        var list = Queue.GetOrNew(hive);
        if (list.Count <= 0)
            return;

        var larva = EntityQueryEnumerator<LarvaQueuedComponent>();
        while (larva.MoveNext(out var ent, out var comp))
        {
            if (_mobState.IsDead(ent))
            {
                RemCompDeferred<LarvaQueuedComponent>(ent);
                continue;
            }

            if (!_hive.IsMember(ent, hive))
                return;

            if (TryPopNextSessionInQueue(hive, out var session))
            {
                var newMind = _mind.CreateMind(session.UserId, Name(ent));
                _mind.TransferTo(newMind, ent, ghostCheckOverride: true);
                RemCompDeferred<LarvaQueuedComponent>(ent);
            }
            else
            {
                break; // no more players in queue
            }
        }
    }

    private void BurrowedLarvaSpawn(Entity<HiveComponent> hive)
    {
        var list = Queue.GetOrNew(hive);
        if (list.Count <= 0)
            return;

        var amount = hive.Comp.BurrowedLarva;
        if (amount <= 0)
            return;

        for (var i = amount; i >= 0; i--)
        {
            if (TryPopNextSessionInQueue(hive, out var session))
            {
                _hive.JoinBurrowedLarva(hive, session);
            }
            else
            {
                break; // no more players in queue
            }
        }
    }

    private bool TryPopNextSessionInQueue(Entity<HiveComponent> hive, [NotNullWhen(true)] out ICommonSession? nextSession)
    {
        nextSession = null;
        var list = Queue.GetOrNew(hive);
        if (list.Count <= 0)
            return false;

        // TODO RMC14 keep spot of disconnected players
        for (var node = list.First; node != null;)
        {
            var next = node.Next;
            var netId = node.Value;
            list.Remove(node);

            if (_player.TryGetSessionById(netId, out var session) && session.AttachedEntity != null)
            {
                nextSession = session;
                return true;
            }

            node = next;
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
        if (PreQueue.GetOrNew(hive).Remove(netUserId))
            return true;

        if (Queue.GetOrNew(hive).Remove(netUserId))
            return true;

        return false;
    }

    /// <summary>
    /// This will remove them from all queues.
    /// </summary>
    /// <param name="actor">The actor component of the entity</param>
    private void RemoveFromAllQueues(ActorComponent actor)
    {
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            TryRemoveFromQueue(actor.PlayerSession.UserId, (hiveId, hive));
        }
    }

    private void RemoveFromAllQueues(NetUserId netUserId)
    {
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            TryRemoveFromQueue(netUserId, (hiveId, hive));
        }
    }

    public bool IsQueuedFor(NetUserId netUserId, EntityUid hiveId)
    {
        if (PreQueue.TryGetValue(hiveId, out var preQueueUsers)
            && preQueueUsers.ContainsKey(netUserId))
            return true;

        if (Queue.TryGetValue(hiveId, out var queueUsers)
            && queueUsers.Contains(netUserId))
            return true;

        return false;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _gameTiming.CurTime;

        // Checking each pre queue to see if anyone has finished their timer.
        foreach (var (hive, hivePreQueue) in PreQueue)
        {
            foreach (var (userId, userTime) in hivePreQueue)
            {
                if (userTime >= time.TotalSeconds)
                    continue;
                Queue.GetOrNew(hive).AddLast(userId);

                if (_player.TryGetSessionById(userId, out var session) && session.AttachedEntity != null)
                {
                    _popup.PopupEntity(
                        $"You have been added to the queue at position {Queue.GetOrNew(hive).Count}",
                        session.AttachedEntity.Value, session.AttachedEntity.Value);
                }

                hivePreQueue.Remove(userId);
            }

            SpawnLarva(hive);
        }
    }
}
