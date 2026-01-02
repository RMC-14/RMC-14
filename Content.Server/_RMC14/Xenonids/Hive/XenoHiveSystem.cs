using Content.Server._RMC14.Announce;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Data;
using IConfigurationManager = Robust.Shared.Configuration.IConfigurationManager;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class XenoHiveSystem : SharedXenoHiveSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;

    private readonly List<string> _announce = [];
    private readonly EntProtoId _defaultHive = "CMXenoHive";

    private TimeSpan _lateJoinsPerBurrowedLarvaEarlyThreshold;
    private float _lateJoinsPerBurrowedLarvaEarly;
    private float _lateJoinsPerBurrowedLarva;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<HijackBurrowedSurgeComponent, ComponentStartup>(OnBurrowedSurgeStartup);
        SubscribeLocalEvent<HijackBurrowedSurgeComponent, ComponentShutdown>(OnBurrowedSurgeShutdown);

        Subs.CVar(_config,
            RMCCVars.RMCLateJoinsPerBurrowedLarvaEarlyThresholdMinutes,
            v => _lateJoinsPerBurrowedLarvaEarlyThreshold = TimeSpan.FromMinutes(v),
            true);
        Subs.CVar(_config, RMCCVars.RMCLateJoinsPerBurrowedLarvaEarly, v => _lateJoinsPerBurrowedLarvaEarly = v, true);
        Subs.CVar(_config, RMCCVars.RMCLateJoinsPerBurrowedLarva, v => _lateJoinsPerBurrowedLarva = v, true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin || !HasComp<MarineComponent>(ev.Mob))
            return;

        if (HasComp<RMCAdminSpawnedComponent>(ev.Mob))
            return;

        if (ev.JobId is not { } jobId ||
            !_prototypes.TryIndex(jobId, out JobPrototype? job) ||
            job.RoleWeight < 0)
        {
            return;
        }

        var time = _timing.CurTime;
        var lateJoinsPer = time < _lateJoinsPerBurrowedLarvaEarlyThreshold
            ? _lateJoinsPerBurrowedLarvaEarly
            : _lateJoinsPerBurrowedLarva;

        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var uid, out var hive))
        {
            if (!hive.LateJoinGainLarva)
                continue;

            hive.LateJoinMarines += job.RoleWeight;
            if (hive.LateJoinMarines < lateJoinsPer)
                continue;

            hive.LateJoinMarines -= lateJoinsPer;
            IncreaseBurrowedLarva((uid, hive), 1);
        }
    }

    private void OnBurrowedSurgeStartup(Entity<HijackBurrowedSurgeComponent> hive, ref ComponentStartup args)
    {
        _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hive, Loc.GetString("rmc-xeno-burrowed-surge-start"));
    }

    private void OnBurrowedSurgeShutdown(Entity<HijackBurrowedSurgeComponent> hive, ref ComponentShutdown args)
    {
        _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hive, Loc.GetString("rmc-xeno-burrowed-surge-end"));
    }

    public override void Update(float frameTime)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var roundTime = _gameTicker.RoundDuration();
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            _announce.Clear();

            for (var i = 0; i < hive.AnnouncementsLeft.Count; i++)
            {
                var left = hive.AnnouncementsLeft[i];
                if (roundTime >= left)
                {
                    if (hive.Unlocks.TryGetValue(left, out var unlocks))
                    {
                        foreach (var unlock in unlocks)
                        {
                            hive.AnnouncedUnlocks.Add(unlock);

                            if (_prototypes.TryIndex(unlock, out var prototype))
                            {
                                _announce.Add(prototype.Name);
                            }
                        }
                    }

                    hive.AnnouncementsLeft.RemoveAt(i);
                    i--;
                    Dirty(hiveId, hive);
                }
            }

            if (_announce.Count > 0)
            {
                var popup = Loc.GetString("rmc-hive-supports-castes", ("castes", string.Join(", ", _announce)));
                _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hiveId, popup, hive.AnnounceSound, PopupType.Large);
                EvoScreech(hive);
            }

            if (!hive.AnnouncedQueenDeathCooldownOver && hive.CurrentQueen == null && hive.NewQueenAt.HasValue && roundTime >= hive.NewQueenAt.Value)
            {
                var queenPopup = Loc.GetString("rmc-queen-death-cooldown-over");
                _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hiveId, queenPopup, hive.AnnounceSound);
                hive.AnnouncedQueenDeathCooldownOver = true;
                Dirty(hiveId, hive);
            }

            if (!hive.AnnouncedHiveCoreCooldownOver && hive.NewCoreAt.HasValue && roundTime >= hive.NewCoreAt)
            {
                var corePopup = Loc.GetString("rmc-hive-core-cooldown-over");
                _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hiveId, corePopup, hive.AnnounceSound);
                hive.AnnouncedHiveCoreCooldownOver = true;
                Dirty(hiveId, hive);
            }

        }

        var time = _timing.CurTime;
        var surge = EntityQueryEnumerator<HijackBurrowedSurgeComponent, HiveComponent>();
        while (surge.MoveNext(out var id, out var burrowed, out var hive))
        {
            if (time < burrowed.NextSurgeAt)
                continue;

            if (GetHiveCore((id, hive)) == null)
            {
                //Reset time between if no core
                if (burrowed.SurgeEvery != burrowed.ResetSurgeTime)
                    burrowed.SurgeEvery = burrowed.ResetSurgeTime;
                continue;
            }

            IncreaseBurrowedLarva(1);
            burrowed.PooledLarva--;
            if (burrowed.PooledLarva < 1)
                RemCompDeferred<HijackBurrowedSurgeComponent>(id);

            if (burrowed.SurgeEvery > burrowed.MinSurgeTime)
                burrowed.SurgeEvery -= burrowed.ReduceSurgeBy;

            burrowed.NextSurgeAt = time + burrowed.SurgeEvery;

        }
    }

    /// <summary>
    /// Create a new hive with a name.
    /// </summary>
    public EntityUid CreateHive(string name, EntProtoId? proto = null)
    {
        var ent = Spawn(proto ?? _defaultHive);
        _metaData.SetEntityName(ent, name);
        return ent;
    }

    public void EvoScreech(HiveComponent hive)
    {
        if (hive.CurrentQueen is not { } queen)
            return;

        // Get the map that the queen is on
        var map = _transform.GetMapId(queen);
        var mapFilter = Filter.BroadcastMap(map);

        foreach (var session in mapFilter.Recipients)
        {
            if (session.AttachedEntity is not { } recipient)
                continue;

            if (HasComp<XenoComponent>(recipient))
                continue;

            var popupText = Loc.GetString(HasComp<SynthComponent>(recipient)
                ? "rmc-hive-supports-castes-synth"
                : "rmc-hive-supports-castes-human");

            popupText = $"[bold][font size=24][color=red]\n{popupText}\n[/color][/font][/bold]";

            _audio.PlayEntity(hive.MarineAnnounceSound, recipient, recipient);
            _rmcChat.ChatMessageToOne(ChatChannel.Radio, popupText, popupText, default, false, session.Channel);
        }
    }
}
