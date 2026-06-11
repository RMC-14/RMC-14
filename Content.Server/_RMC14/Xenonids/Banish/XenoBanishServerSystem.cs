using Content.Shared._RMC14.Xenonids.Banish;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Banish;

public sealed class XenoBanishServerSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBanishComponent, XenoBanishedEvent>(OnXenoBanishedEvent);
        SubscribeLocalEvent<XenoBanishComponent, XenoReadmittedEvent>(OnXenoReadmittedEvent);
        SubscribeLocalEvent<XenoBanishComponent, MobStateChangedEvent>(OnBanishedMobStateChanged);
    }

    private void OnXenoBanishedEvent(Entity<XenoBanishComponent> ent, ref XenoBanishedEvent args)
    {
        if (TryComp(ent, out ActorComponent? actor))
            OnXenoBanished(ent, actor.PlayerSession.UserId);
    }

    private void OnXenoReadmittedEvent(Entity<XenoBanishComponent> ent, ref XenoReadmittedEvent args)
    {
        if (TryComp(ent, out ActorComponent? actor))
            OnXenoReadmitted(ent, actor.PlayerSession.UserId);
    }

    private void OnBanishedMobStateChanged(Entity<XenoBanishComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.Banished || args.NewMobState != MobState.Dead)
            return;

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
        if (_hive.GetHive(xeno) is not { } hive)
            return;

        hive.Comp.BanishedPlayers[userId] = _timing.CurTime + TimeSpan.FromMinutes(30);
        Dirty(hive);
    }

    public void OnXenoReadmitted(EntityUid xeno, Guid userId)
    {
        if (_hive.GetHive(xeno) is not { } hive)
            return;

        hive.Comp.BanishedPlayers.Remove(userId);
        Dirty(hive);
    }

    public bool CanTakeXenoRole(Guid userId, EntityUid hive)
    {
        if (!TryComp<HiveComponent>(hive, out var hiveComp))
            return true;

        return !hiveComp.BanishedPlayers.ContainsKey(userId);
    }

    public TimeSpan? GetBanishTimeRemaining(Guid userId, EntityUid hive)
    {
        if (!TryComp<HiveComponent>(hive, out var hiveComp))
            return null;

        if (!hiveComp.BanishedPlayers.TryGetValue(userId, out var unbanishTime))
            return null;

        var remaining = unbanishTime - _timing.CurTime;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public override void Update(float frameTime)
    {
        var currentTime = _timing.CurTime;
        var toRemove = new List<(EntityUid, Guid)>();

        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            foreach (var (userId, unbanishTime) in hive.BanishedPlayers)
            {
                if (currentTime >= unbanishTime)
                    toRemove.Add((hiveId, userId));
            }
        }

        foreach (var (hiveId, userId) in toRemove)
        {
            if (TryComp<HiveComponent>(hiveId, out var hive))
            {
                hive.BanishedPlayers.Remove(userId);
                Dirty(hiveId, hive);
            }
        }
    }
}
