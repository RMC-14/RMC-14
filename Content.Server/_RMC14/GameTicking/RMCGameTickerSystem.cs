using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.GameTicking;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.GameTicking;

public sealed class RMCGameTickerSystem : SharedRMCGameTickerSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private bool _autoReady;
    private readonly HashSet<ICommonSession> _autoReadyPending = new();

    public override IReadOnlyDictionary<NetUserId, PlayerGameStatus> PlayerGameStatuses => _gameTicker.PlayerGameStatuses;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);

        Subs.CVar(_config, RMCCVars.RMCLobbyAutoReady, v => _autoReady = v, true);
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        var rmcEv = new RMCPlayerJoinedLobbyEvent(ev.PlayerSession);
        RaiseLocalEvent(ref rmcEv);

        // readied in update, a round restart moves players to the lobby before it resets their ready status
        if (_autoReady)
            _autoReadyPending.Add(ev.PlayerSession);
    }

    public override void Update(float frameTime)
    {
        if (_autoReadyPending.Count == 0)
            return;

        _autoReadyPending.RemoveWhere(TryAutoReady);
    }

    private bool TryAutoReady(ICommonSession session)
    {
        if (session.Status == SessionStatus.Disconnected)
            return true;

        // readying is refused until the player's preferences have loaded
        if (!_userDb.IsLoadComplete(session))
            return false;

        _gameTicker.ToggleReady(session, true);
        return true;
    }

    public override void PlayerJoinGame(ICommonSession session, bool silent = false)
    {
        base.PlayerJoinGame(session);
        _gameTicker.PlayerJoinGame(session, silent);
    }

    public override bool ServerOnlyIsInRound()
    {
        return _gameTicker.RunLevel == GameRunLevel.InRound;
    }
}
