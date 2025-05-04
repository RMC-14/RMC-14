using Content.Server.GameTicking;
using Content.Shared._RMC14.GameTicking;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.GameTicking;

public sealed class RMCGameTickerSystem : SharedRMCGameTickerSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override IReadOnlyDictionary<NetUserId, PlayerGameStatus> PlayerGameStatuses => _gameTicker.PlayerGameStatuses;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        var rmcEv = new RMCPlayerJoinedLobbyEvent(ev.PlayerSession);
        RaiseLocalEvent(ref rmcEv);
    }

    public override void PlayerJoinGame(ICommonSession session, bool silent = false)
    {
        base.PlayerJoinGame(session);
        _gameTicker.PlayerJoinGame(session, silent);
    }
}
