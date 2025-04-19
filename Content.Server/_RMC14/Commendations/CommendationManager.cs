using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.Commendations;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Commendations;

public sealed class CommendationManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, List<Commendation>> _commendations = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var commendations = await _db.GetCommendationsReceived(player.UserId);
        _commendations[player.UserId] = commendations
            .Select(c => new Commendation(c.GiverName, c.ReceiverName, c.Name, c.Text, c.Type, c.RoundId))
            .ToList();
    }

    private void FinishLoad(ICommonSession player)
    {
        SendCommendations(player);
    }

    private void SendCommendations(ICommonSession player)
    {
        if (!_commendations.TryGetValue(player.UserId, out var commendations))
            return;

        var msg = new CommendationsMsg { Commendations = commendations.ToList() };
        _net.ServerSendMessage(msg, player.Channel);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _commendations.Remove(player.UserId);
    }

    public void CommendationAdded(NetUserId receiver, Commendation commendation)
    {
        if (!_commendations.TryGetValue(receiver, out var commendations))
            return;

        commendations.Add(commendation);

        if (_player.TryGetSessionById(receiver, out var receiverSession))
            SendCommendations(receiverSession);
    }

    public void PostInject()
    {
        _net.RegisterNetMessage<CommendationsMsg>();

        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
