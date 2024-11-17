using Content.Server._RMC14.Announce;
using Content.Server.GameTicking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using IConfigurationManager = Robust.Shared.Configuration.IConfigurationManager;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class XenoHiveSystem : SharedXenoHiveSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;

    private readonly List<string> _announce = [];
    private readonly EntProtoId _defaultHive = "CMXenoHive";

    private TimeSpan _lateJoinsPerBurrowedLarvaEarlyThreshold;
    private float _lateJoinsPerBurrowedLarvaEarly;
    private float _lateJoinsPerBurrowedLarva;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

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
            hive.BurrowedLarva++;
            Dirty(uid, hive);
        }
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

            if (_announce.Count == 0)
                continue;

            var popup = Loc.GetString("rmc-hive-supports-castes", ("castes", string.Join(", ", _announce)));
            _xenoAnnounce.AnnounceToHive(EntityUid.Invalid, hiveId, popup, hive.AnnounceSound, PopupType.Large);
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
}
