using System.Collections;
using System.Linq;
using Content.Shared._RMC14.Admin.AdminGhost;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

public sealed class LarvaQueueSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;

    private static readonly Dictionary<Entity<HiveComponent>, List<NetUserId>> Queue = new();
    private static readonly Dictionary<Entity<HiveComponent>, Dictionary<NetUserId, double>> PreQueue = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<JoinXenoComponent, JoinLarvaQueueEvent>(OnJoinLarvaQueue);
        SubscribeLocalEvent<ActorComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        Queue.Clear();
        PreQueue.Clear();
    }

    private void OnMindAdded(Entity<ActorComponent> actor, ref MindAddedMessage ev)
    {
        if (_net.IsClient)
            return;

        //Ignore when someone aGhosts surely this will have no consequences
        if (HasComp<RMCAdminGhostComponent>(ev.Container.Owner))
            return;

        RemoveFromAllQueues(actor);
    }

    private void OnJoinLarvaQueue(Entity<JoinXenoComponent> ent, ref JoinLarvaQueueEvent args)
    {
        if (_net.IsClient)
            return;

        var denyQueuing = _config.GetCVar(RMCCVars.RMCLarvaQueueRoundstartDelaySeconds);
        if (_gameTicker.RoundDuration().TotalSeconds <= denyQueuing)
            return;

        if (!TryComp(ent, out ActorComponent? actor) || !TryComp(ent, out GhostComponent? ghost) || !TryGetEntity(args.Hive, out var hiveUid) || !TryComp(hiveUid, out HiveComponent? hiveComp))
            return;

        var hive = new Entity<HiveComponent>(hiveUid.Value, hiveComp);
        var session = actor.PlayerSession.UserId;
        var actorEntity = actor.PlayerSession.AttachedEntity;
        var larvaWaitTime = _config.GetCVar(RMCCVars.RMCLarvaQueueWaitSeconds);

        if (actorEntity == null)
            return;

        if (RemoveFromQueue(actor, hive))
        {
            _popup.PopupEntity($"You have been removed from the queue", actorEntity.Value);
            return;
        }

        if ((_gameTiming.CurTime - ghost.TimeOfDeath).Seconds >= larvaWaitTime)
        {
            Queue.GetOrNew(hive).Add(session);
            _popup.PopupEntity($"You have been added to the queue in the {Queue.GetOrNew(hive).Count} position", actorEntity.Value);
        }
        else
        {
            PreQueue.GetOrNew(hive).Add(session, larvaWaitTime + ghost.TimeOfDeath.TotalSeconds);
            _popup.PopupEntity($"You have been added to the Pre-queue, until you have waited the required {larvaWaitTime} seconds", actorEntity.Value);
        }
    }

    // private bool SpawnAsLarva(ICommonSession session)
    // {
    //     if (!TryGetEntity(args.Hive, out var hive) ||
    //         !TryComp(hive, out HiveComponent? hiveComp) ||
    //         !TryComp(ent, out ActorComponent? actor))
    //     {
    //         return false;
    //     }
    //
    //     _hive.JoinBurrowedLarva((hive.Value, hiveComp), actor.PlayerSession);
    // }

    /// <summary>
    /// This will try and remove them from a queue and return true if they are part of that queue.
    /// </summary>
    /// <param name="actor">The actor component of the entity</param>
    /// <param name="hive">The hive that we are removing them from the queue of</param>
    /// <returns></returns>

    private bool RemoveFromQueue(ActorComponent actor, Entity<HiveComponent> hive)
    {
        if (PreQueue.GetOrNew(hive).Remove(actor.PlayerSession.UserId))
            return true;

        if (Queue.GetOrNew(hive).Remove(actor.PlayerSession.UserId))
            return true;

        return false;
    }

    /// <summary>
    /// This will remove them from all queues.
    /// </summary>
    /// <param name="actor">The actor component of the entity</param>
    private void RemoveFromAllQueues(Entity<ActorComponent> actor)
    {
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            RemoveFromQueue(actor, (hiveId, hive));
        }
    }
}
