using System.Collections;
using System.Linq;
using Content.Shared._RMC14.Admin.AdminGhost;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

public sealed class LarvaQueueSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;


    private static List<ICommonSession> _queue = new();

    private static Dictionary<ICommonSession, TimeSpan> _preQueue = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<JoinXenoComponent, JoinLarvaQueueEvent>(OnJoinLarvaQueue);
        SubscribeLocalEvent<ActorComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<ActorComponent> actor, ref MindAddedMessage ev)
    {
        if(_net.IsClient)
            return;

        if (HasComp<RMCAdminGhostComponent>(ev.Container.Owner))
            return;

        RemoveFromQueue(actor);
    }

    private void OnJoinLarvaQueue(Entity<JoinXenoComponent> ent, ref JoinLarvaQueueEvent args)
    {
        if(_net.IsClient)
            return;

        if(!TryComp(ent, out ActorComponent? actor) || !TryComp(ent, out GhostComponent? ghost))
            return;

        if (RemoveFromQueue(actor))
            return;

        var session = actor.PlayerSession;

        if ((_gameTiming.CurTime - ghost.TimeOfDeath).Seconds >= _cfg.GetCVar(RMCCVars.RMCMinimumWaitForLarva))
        {
            _queue.Add(session);
        }
        else
        {
            _preQueue.Add(session, ghost.TimeOfDeath);
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

    private bool RemoveFromQueue(ActorComponent actor)
    {
        if (_preQueue.Remove(actor.PlayerSession))
            return true;

        if (_queue.Remove(actor.PlayerSession))
            return true;

        return false;
    }
}
