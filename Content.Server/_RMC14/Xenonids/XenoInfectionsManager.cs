using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids;

/// <summary>
/// Caches each connected player's total successful parasite infections (from the database) for the
/// duration of their session, both for ranking the parasite role and for displaying the count client-side.
/// </summary>
public sealed class XenoInfectionsManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, int> _infects = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var count = await _db.GetParasiteInfects(player.UserId.UserId);
        cancel.ThrowIfCancellationRequested();
        _infects[player.UserId] = count;
    }

    private void FinishLoad(ICommonSession player)
    {
        SendInfects(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _infects.Remove(player.UserId);
    }

    private void SendInfects(ICommonSession player)
    {
        var msg = new RMCParasiteInfectionsMsg
        {
            Infections = GetInfects(player.UserId),
        };
        _net.ServerSendMessage(msg, player.Channel);
    }

    public int GetInfects(NetUserId player)
    {
        return _infects.GetValueOrDefault(player, 0);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<RMCParasiteInfectionsMsg>();
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
