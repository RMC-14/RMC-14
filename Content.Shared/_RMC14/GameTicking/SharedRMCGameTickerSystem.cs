using System.Collections.Immutable;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.GameTicking;

public abstract class SharedRMCGameTickerSystem : EntitySystem
{
    public virtual IReadOnlyDictionary<NetUserId, PlayerGameStatus> PlayerGameStatuses =>
        ImmutableDictionary<NetUserId, PlayerGameStatus>.Empty;

    public virtual void PlayerJoinGame(ICommonSession session, bool silent = false)
    {
    }
}
