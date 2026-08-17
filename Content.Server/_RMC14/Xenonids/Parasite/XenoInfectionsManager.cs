using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.Xenonids.Parasite;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Parasite;

/// <summary>
/// Caches each connected player's total successful parasite infections (from the database) for the
/// duration of their session, both for ranking the parasite role and for displaying the count client-side.
/// </summary>
public sealed class XenoInfectionsManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<ICommonSession, int> _infects = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var count = await _db.GetParasiteInfects(player.UserId);
        cancel.ThrowIfCancellationRequested();
        _infects[player] = count;
    }

    private void FinishLoad(ICommonSession player)
    {
        SendInfects(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _infects.Remove(player);
    }

    private void SendInfects(ICommonSession player)
    {
        var msg = new RMCParasiteInfectionsMsg
        {
            Infections = GetInfects(player),
        };
        _net.ServerSendMessage(msg, player.Channel);
    }

    public int GetInfects(ICommonSession player)
    {
        return _infects.GetValueOrDefault(player, 0);
    }

    /// <summary>
    /// Records a successful infection in the session cache and pushes the new total to the client, so the
    /// displayed count (and any subsequently spawned parasite's rank) reflects infections made this round.
    /// </summary>
    public void IncreaseInfects(ICommonSession player)
    {
        _infects[player] = GetInfects(player) + 1;
        SendInfects(player);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<RMCParasiteInfectionsMsg>();
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
