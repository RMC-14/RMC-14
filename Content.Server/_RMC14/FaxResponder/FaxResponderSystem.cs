using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.GameTicking;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.FaxResponder;
using Content.Shared._RMC14.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.FaxResponder;

public sealed class FaxResponderSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    // Jobs that show up in the lobby button window
    private static readonly ProtoId<JobPrototype>[] VisibleJobs =
    [
        "CMWeYaresponder",
        "CMFreePressResponder",
    ];

    // Jobs that have already been claimed this round, one player each
    private readonly HashSet<ProtoId<JobPrototype>> _taken = new();

    public override void Initialize()
    {
        base.Initialize();

        // Run before the distress signal and default spawn systems, otherwise they put responders on the ship
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning,
            before: [typeof(CMDistressSignalRuleSystem), typeof(SpawnPointSystem)]);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<RMCPlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        SubscribeNetworkEvent<FaxResponderStatusRequest>(OnStatusRequest);
        SubscribeNetworkEvent<JoinFaxResponderRequest>(OnJoinRequest);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        if (args.Job == null)
            return;

        // Find matching fax responder spawn point
        var query = EntityQueryEnumerator<FaxResponderSpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (query.MoveNext(out _, out var comp, out var xform))
        {
            if (comp.Job == args.Job)
                possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count == 0)
            return;

        var spawnLoc = _random.Pick(possiblePositions);
        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null || Array.IndexOf(VisibleJobs, (ProtoId<JobPrototype>) ev.JobId) < 0)
            return;

        if (_taken.Add(ev.JobId))
            RaiseNetworkEvent(GetStatus());

        RaiseNetworkEvent(new FaxResponderRulesEvent(), ev.Player);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _taken.Clear();
    }

    private void OnPlayerJoinedLobby(ref RMCPlayerJoinedLobbyEvent ev)
    {
        RaiseNetworkEvent(GetStatus(), ev.Player);
    }

    private void OnStatusRequest(FaxResponderStatusRequest msg, EntitySessionEventArgs args)
    {
        RaiseNetworkEvent(GetStatus(), args.SenderSession);
    }

    private void OnJoinRequest(JoinFaxResponderRequest msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        if (!_gameTicker.PlayerGameStatuses.TryGetValue(session.UserId, out var status) ||
            status == PlayerGameStatus.JoinedGame)
        {
            return;
        }

        if (Array.IndexOf(VisibleJobs, msg.Job) < 0 || _taken.Contains(msg.Job))
            return;

        // Without a spawn point the default spawn system would put them on the ship instead
        if (!HasSpawnPoint(msg.Job))
            return;

        // Handles mind, job role, bans, whitelist and playtime checks.
        // OnPlayerSpawning places them at the fax responder spawn point.
        _gameTicker.MakeJoinGame(session, EntityUid.Invalid, msg.Job);
    }

    private bool HasSpawnPoint(ProtoId<JobPrototype> job)
    {
        var query = EntityQueryEnumerator<FaxResponderSpawnPointComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.Job == job)
                return true;
        }

        return false;
    }

    private FaxResponderStatusEvent GetStatus()
    {
        var jobs = new List<FaxResponderJobStatus>();
        foreach (var job in VisibleJobs)
        {
            if (!HasSpawnPoint(job))
                continue;

            jobs.Add(new FaxResponderJobStatus(job, _taken.Contains(job)));
        }

        return new FaxResponderStatusEvent(jobs);
    }
}
