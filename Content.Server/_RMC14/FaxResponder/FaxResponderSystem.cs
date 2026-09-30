using Content.Server.Mind;
using Content.Server.Players.JobWhitelist;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.FaxResponder;
using Content.Shared._RMC14.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.FaxResponder;

public sealed class FaxResponderSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedRMCGameTickerSystem _rmcGameTicker = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private static readonly ProtoId<JobPrototype> WeyaJob = "CMWeYaresponder";
    private static readonly ProtoId<JobPrototype> FreePressJob = "CMFreePressResponder";

    private int _weyaSlotsRemaining = 1;
    private int _freePressSlotsRemaining = 1;

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<FaxResponderStatusEvent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<RMCPlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);

        SubscribeNetworkEvent<FaxResponderStatusRequest>(OnStatusRequest);
        SubscribeNetworkEvent<JoinFaxResponderRequest>(OnJoinRequest);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _weyaSlotsRemaining = 1;
        _freePressSlotsRemaining = 1;
        SendStatusToAll();
    }

    private void OnPlayerJoinedLobby(ref RMCPlayerJoinedLobbyEvent ev)
    {
        SendStatus(ev.Player);
    }

    private void OnStatusRequest(FaxResponderStatusRequest msg, EntitySessionEventArgs args)
    {
        SendStatus(args.SenderSession);
    }

    private void OnJoinRequest(JoinFaxResponderRequest msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        // Check player is in lobby, not already in game
        if (!_rmcGameTicker.PlayerGameStatuses.TryGetValue(session.UserId, out var status) ||
            status == PlayerGameStatus.JoinedGame)
        {
            return;
        }

        if (!_jobWhitelist.IsAllowed(session, msg.Job))
            return;

        ref var slots = ref msg.Job == WeyaJob ? ref _weyaSlotsRemaining : ref _freePressSlotsRemaining;
        if (slots <= 0)
            return;

        var spawnPoint = FindSpawnPoint(msg.Job);
        if (spawnPoint == null)
            return;

        slots--;

        var profile = GetProfile(session);
        var coordinates = Transform(spawnPoint.Value).Coordinates;
        var mob = _stationSpawning.SpawnPlayerMob(coordinates, msg.Job, profile, null);

        if (!_mind.TryGetMind(session.UserId, out var mind))
            mind = _mind.CreateMind(session.UserId);

        _mind.TransferTo(mind.Value, mob);
        _rmcGameTicker.PlayerJoinGame(session);

        SendStatusToAll();
    }

    private EntityUid? FindSpawnPoint(ProtoId<JobPrototype> job)
    {
        var query = EntityQueryEnumerator<FaxResponderSpawnPointComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Job == job)
                return uid;
        }

        return null;
    }

    private HumanoidCharacterProfile? GetProfile(ICommonSession session)
    {
        return (HumanoidCharacterProfile?) _prefs.GetPreferences(session.UserId).SelectedCharacter;
    }

    private void SendStatus(ICommonSession session)
    {
        var msg = new FaxResponderStatusEvent
        {
            WeyaSlots = _weyaSlotsRemaining,
            FreePressSlots = _freePressSlotsRemaining
        };
        _net.ServerSendMessage(msg, session.Channel);
    }

    private void SendStatusToAll()
    {
        var msg = new FaxResponderStatusEvent
        {
            WeyaSlots = _weyaSlotsRemaining,
            FreePressSlots = _freePressSlotsRemaining
        };

        foreach (var session in _player.Sessions)
        {
            if (_rmcGameTicker.PlayerGameStatuses.TryGetValue(session.UserId, out var status) &&
                status != PlayerGameStatus.JoinedGame)
            {
                _net.ServerSendMessage(msg, session.Channel);
            }
        }
    }
}
