using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids;

public sealed class XenoInfectionsManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, int> _infects = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var count = await _db.GetParasiteInfects(player.UserId.UserId);
        cancel.ThrowIfCancellationRequested();
        _infects[player.UserId] = count;
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _infects.Remove(player.UserId);
    }

    public int GetInfects(NetUserId player)
    {
        return _infects.GetValueOrDefault(player, 0);
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
