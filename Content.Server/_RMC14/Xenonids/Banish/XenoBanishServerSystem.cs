using Content.Server._RMC14.Xenonids;
using Content.Server._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Banish;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Xenonids.Banish;

public sealed class XenoBanishServerSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<Guid, TimeSpan> _banishedPlayers = new();
    private readonly Dictionary<Guid, TimeSpan> _delayedLarvaSpawns = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBanishComponent, XenoBanishedEvent>(OnXenoBanishedEvent);
        SubscribeLocalEvent<XenoBanishComponent, XenoReadmittedEvent>(OnXenoReadmittedEvent);
        SubscribeLocalEvent<XenoBanishComponent, MobStateChangedEvent>(OnBanishedMobStateChanged);
        SubscribeLocalEvent<XenoBanishComponent, ComponentShutdown>(OnBanishShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnXenoBanishedEvent(Entity<XenoBanishComponent> ent, ref XenoBanishedEvent args)
    {
        if (TryComp(ent, out ActorComponent? actor))
        {
            OnXenoBanished(ent, actor.PlayerSession.UserId);
        }
    }

    private void OnXenoReadmittedEvent(Entity<XenoBanishComponent> ent, ref XenoReadmittedEvent args)
    {
        if (TryComp(ent, out ActorComponent? actor))
        {
            OnXenoReadmitted(ent, actor.PlayerSession.UserId);
        }
    }

    private void OnBanishedMobStateChanged(Entity<XenoBanishComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.Banished || args.NewMobState != MobState.Dead)
            return;

        // RMC14: Lesser drones don't give larva when banished
        if (HasComp<XenoHiveCoreRoleComponent>(ent.Owner))
            return;

        // When a banished xeno dies, add a burrowed larva after 5 minutes
        if (_hive.GetHive(ent.Owner) is { } hive)
        {
            Timer.Spawn(TimeSpan.FromMinutes(5), () =>
            {
                _hive.IncreaseBurrowedLarva(hive, 1);
            });
        }
    }

    public void OnXenoBanished(EntityUid xeno, Guid userId)
    {
        _banishedPlayers[userId] = _timing.CurTime;
        _delayedLarvaSpawns[userId] = _timing.CurTime + TimeSpan.FromMinutes(5);
        
        // Schedule automatic unbanish after 30 minutes
        Timer.Spawn(TimeSpan.FromMinutes(30), () =>
        {
            _banishedPlayers.Remove(userId);
            
            if (Exists(xeno) && TryComp(xeno, out XenoBanishComponent? comp) && comp.Banished)
            {
                var banishSystem = EntityManager.System<XenoBanishSystem>();
                banishSystem.UnbanishXeno(xeno);
            }
        });
    }

    public void OnXenoReadmitted(EntityUid xeno, Guid userId)
    {
        _banishedPlayers.Remove(userId);
        _delayedLarvaSpawns.Remove(userId);
    }

    private void OnBanishShutdown(Entity<XenoBanishComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp(ent, out ActorComponent? actor))
        {
            var userId = actor.PlayerSession.UserId;
            _banishedPlayers.Remove(userId);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _banishedPlayers.Clear();
        _delayedLarvaSpawns.Clear();
    }

    public override void Update(float frameTime)
    {
        var currentTime = _timing.CurTime;
        
        // Clean up expired delayed larva spawns
        var toRemove = new List<Guid>();
        foreach (var (userId, spawnTime) in _delayedLarvaSpawns)
        {
            if (currentTime >= spawnTime)
                toRemove.Add(userId);
        }

        foreach (var userId in toRemove)
        {
            _delayedLarvaSpawns.Remove(userId);
        }
    }
}