using System.Collections;
using System.Linq;
using Content.Shared._RMC14.Admin.AdminGhost;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
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

    private static readonly EntProtoId Lesser = "CMXenoLesserDrone";

    [ViewVariables]
    public static readonly Dictionary<Entity<HiveComponent>, List<NetUserId>> Queue = new();

    [ViewVariables]
    public static readonly Dictionary<Entity<HiveComponent>, Dictionary<NetUserId, double>> PreQueue = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<JoinXenoComponent, JoinLarvaQueueEvent>(OnJoinLarvaQueue);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<BurrowedLarvaAddedEvent>(OnBurrowedLarvaAdded);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<HiveMemberComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindRemoved(Entity<HiveMemberComponent> ent, ref MindRemovedMessage args)
    {
        if (!TryComp(ent, out XenoComponent? xeno))
            return;

        if (HasComp<XenoParasiteComponent>(ent) || !TryPrototype(ent, out var lesserProto) ||
            lesserProto.ID == Lesser || HasComp<DropshipHijackerComponent>(ent))
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

        if (RemoveFromQueue(actor.PlayerSession.UserId, hive))
        {
            _popup.PopupEntity($"You have been removed from the queue", actorEntity.Value);
            return;
        }

        if ((_gameTiming.CurTime - ghost.TimeOfDeath).TotalSeconds >= larvaWaitTime)
        {
            Queue.GetOrNew(hive).Add(session);
            _popup.PopupEntity($"You have been added to the queue in the {Queue.GetOrNew(hive).Count} position",
                actorEntity.Value);
        }
        else
        {
            PreQueue.GetOrNew(hive).Add(session, larvaWaitTime + ghost.TimeOfDeath.TotalSeconds);
            _popup.PopupEntity(
                $"You have been added to the Pre-queue, until you have waited the required {larvaWaitTime} seconds",
                actorEntity.Value);
        }
    }

    private void OnBurrowedLarvaAdded(BurrowedLarvaAddedEvent ev)
    {
        SpawnLarva(ev.Hive);
    }

    private void SpawnLarva(NetEntity hiveNet)
    {
        var hiveEntity = GetEntity(hiveNet);
        if (!TryComp(hiveEntity, out HiveComponent? hiveComp))
            return;
        var hive = (hiveEntity, hiveComp);
        SpawnLarva(hive);
    }

    private void SpawnLarva(Entity<HiveComponent> hive)
    {
        var list = Queue.GetOrNew(hive);
        var originalLength = list.Count;
        if (list.Count < 0)
            return;

        BurstLarvaSpawn(hive);
        BurrowedLarvaSpawn(hive);

        //Popup to people in queue if the queue length changed.
        if (originalLength >= list.Count)
            return;

        foreach (var netId in list)
        {
            if (_player.TryGetSessionById(netId, out var session) && session.AttachedEntity != null)
            {
                _popup.PopupEntity($"You are now in the {Queue.GetOrNew(hive).Count} position for larva queue.",
                    session.AttachedEntity.Value);
            }
        }
    }

    private void BurstLarvaSpawn(Entity<HiveComponent> hive)
    {
        var list = Queue.GetOrNew(hive);
        if (list.Count < 0)
            return;

        var larva = EntityQueryEnumerator<LarvaQueuedComponent>();
        while (larva.MoveNext(out var ent, out var comp))
        {
            if (_mobState.IsDead(ent))
            {
                RemComp<LarvaQueuedComponent>(ent);
                continue;
            }

            if (!TryComp(ent, out HiveMemberComponent? hiveMember) || hiveMember.Hive != hive.Owner)
                return;

            var tryFind = true;
            while (tryFind)
            {
                if (list.Count == 0)
                    return;
                var netId = list.Last();
                list.RemoveAt(list.Count - 1);

                if (!_player.TryGetSessionById(netId, out var session) || session.AttachedEntity == null)
                    continue;

                var newMind = _mind.CreateMind(session.UserId,
                    EntityManager.GetComponent<MetaDataComponent>(ent).EntityName);
                _mind.TransferTo(newMind, ent, ghostCheckOverride: true);
                RemComp<LarvaQueuedComponent>(ent);
                tryFind = true;
            }
        }
    }

    private void BurrowedLarvaSpawn(Entity<HiveComponent> hive)
    {
        var list = Queue.GetOrNew(hive);
        if (list.Count < 0)
            return;

        var amount = hive.Comp.BurrowedLarva;
        if (amount < 1)
            return;

        for (var i = 0; i <= amount; i++)
        {
            if (list.Count == 0)
                return;

            var netId = list.Last();
            list.RemoveAt(list.Count - 1);

            if (_player.TryGetSessionById(netId, out var session) && session.AttachedEntity != null)
            {
                _hive.JoinBurrowedLarva(hive, session);
            }
            else
            {
                i--;
            }
        }
    }

    /// <summary>
    /// This will try and remove them from a queue and return true if they are part of that queue.
    /// </summary>
    /// <param name="netUserId">ID of the person we are removing</param>
    /// <param name="hive">The hive that we are removing them from the queue of</param>
    /// <returns></returns>
    private bool RemoveFromQueue(NetUserId netUserId, Entity<HiveComponent> hive)
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
            RemoveFromQueue(actor.PlayerSession.UserId, (hiveId, hive));
        }
    }

    private void RemoveFromAllQueues(NetUserId netUserId)
    {
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            RemoveFromQueue(netUserId, (hiveId, hive));
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _gameTiming.CurTime;

        // Checking each pre queue to see if anyone has finished their timer.
        foreach (var hive in PreQueue)
        {
            foreach (var user in hive.Value)
            {
                if (user.Value >= time.TotalSeconds)
                    continue;
                Queue.GetOrNew(hive.Key).Add(user.Key);

                if (_player.TryGetSessionById(user.Key, out var session) && session.AttachedEntity != null)
                {
                    _popup.PopupEntity(
                        $"You have been added to the queue in the {Queue.GetOrNew(hive.Key).Count} position",
                        session.AttachedEntity.Value);
                }

                hive.Value.Remove(user.Key);
            }

            SpawnLarva(hive.Key);
        }
    }
}
