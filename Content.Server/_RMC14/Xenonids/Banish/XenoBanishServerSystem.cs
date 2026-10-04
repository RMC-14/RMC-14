using Content.Server._RMC14.Announce;
using Content.Shared._RMC14.Xenonids.Banish;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Banish;

public sealed class XenoBanishServerSystem : EntitySystem
{
    [Dependency] private readonly XenoAnnounceSystem _announce = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBanishedEvent>(OnXenoBanishedEvent);
        SubscribeLocalEvent<XenoReadmittedEvent>(OnXenoReadmittedEvent);
        SubscribeLocalEvent<XenoBanishComponent, MobStateChangedEvent>(OnBanishedMobStateChanged);
    }

    private void OnXenoBanishedEvent(ref XenoBanishedEvent args)
    {
        var banished = args.Banished;

        if (!TryComp<XenoBanishComponent>(banished, out var banishComp))
            return;

        // Add to BanishedPlayers by user ID
        if (TryComp(banished, out ActorComponent? actor) && banishComp.OriginalHive is { } originalHive)
            AddBanishedPlayer(originalHive, actor.PlayerSession.UserId);

        // Announce to hive
        if (banishComp.OriginalHive is { } hive)
        {
            var msg = Loc.GetString("rmc-banish-announcement", ("name", Name(banished)), ("reason", args.Reason));
            _announce.AnnounceSameHiveDefaultSound(args.Banisher, msg);
        }

        // Notify the banished player
        if (TryComp<ActorComponent>(banished, out var banishedActor))
        {
            var banishedMsg = Loc.GetString("rmc-banish-notification", ("reason", args.Reason));
            _popup.PopupEntity(banishedMsg, banished, banishedActor.PlayerSession, PopupType.LargeCaution);
        }
    }

    private void OnXenoReadmittedEvent(ref XenoReadmittedEvent args)
    {
        var readmitted = args.Readmitted;

        // Remove from BanishedPlayers by user ID
        if (TryComp(readmitted, out ActorComponent? actor))
        {
            // Get the hive they're being readmitted to
            if (TryComp<HiveMemberComponent>(readmitted, out var hiveMember) && hiveMember.Hive is { } hive)
                RemoveBanishedPlayer(hive, actor.PlayerSession.UserId);
        }

        // Announce to hive
        if (TryComp<HiveMemberComponent>(readmitted, out var member) && member.Hive is { } readmitHive)
        {
            var msg = Loc.GetString("rmc-readmit-announcement", ("name", Name(readmitted)));
            _announce.AnnounceSameHiveDefaultSound(args.Readmitter, msg);
        }

        // Notify the readmitted player
        if (TryComp<ActorComponent>(readmitted, out var readmittedActor))
        {
            var readmittedMsg = Loc.GetString("rmc-readmit-notification");
            _popup.PopupEntity(readmittedMsg, readmitted, readmittedActor.PlayerSession, PopupType.Large);
        }
    }

    private void OnBanishedMobStateChanged(Entity<XenoBanishComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.Banished || args.NewMobState != MobState.Dead)
            return;

        // When a banished xeno dies, add a burrowed larva to their original hive
        if (ent.Comp.OriginalHive is { } originalHive && TryComp<HiveComponent>(originalHive, out var hiveComp))
        {
            hiveComp.BurrowedLarva++;
            Dirty(originalHive, hiveComp);
        }
    }

    private void AddBanishedPlayer(EntityUid hiveId, Guid userId)
    {
        if (!TryComp<HiveComponent>(hiveId, out var hiveComp))
            return;

        hiveComp.BanishedPlayers[userId] = _timing.CurTime + TimeSpan.FromMinutes(30);
        Dirty(hiveId, hiveComp);
    }

    private void RemoveBanishedPlayer(EntityUid hiveId, Guid userId)
    {
        if (!TryComp<HiveComponent>(hiveId, out var hiveComp))
            return;

        hiveComp.BanishedPlayers.Remove(userId);
        Dirty(hiveId, hiveComp);
    }

    public bool CanTakeXenoRole(Guid userId, EntityUid hive)
    {
        if (!TryComp<HiveComponent>(hive, out var hiveComp))
            return true;

        if (!hiveComp.BanishedPlayers.TryGetValue(userId, out var unbanishTime))
            return true;

        return _timing.CurTime >= unbanishTime;
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
