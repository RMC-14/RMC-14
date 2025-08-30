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

    private readonly Dictionary<NetUserId, (List<Commendation> Received, List<Commendation> Given)> _commendations = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var commendationsReceived = await _db.GetCommendationsReceived(player.UserId);
        var commendationsGiven = await _db.GetCommendationsGiven(player.UserId);
        _commendations[player.UserId] = (ParseCommendations(commendationsReceived), ParseCommendations(commendationsGiven));
    }

    private void FinishLoad(ICommonSession player)
    {
        SendCommendations(player);
    }

    private void SendCommendations(ICommonSession player)
    {
        if (!_commendations.TryGetValue(player.UserId, out var commendations))
            return;

        var msg = new CommendationsMsg
        {
            CommendationsReceived = commendations.Received.ToList(),
            CommendationsGiven = commendations.Given.ToList(),
        };
        _net.ServerSendMessage(msg, player.Channel);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _commendations.Remove(player.UserId);
    }

    public void CommendationAdded(NetUserId giver, NetUserId receiver, Commendation commendation)
    {
        UpdateCommendations(giver, commendation, false);
        UpdateCommendations(receiver, commendation, true);
    }

    public void PostInject()
    {
        _net.RegisterNetMessage<CommendationsMsg>();

        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }

    private List<Commendation> ParseCommendations(List<RMCCommendation> commendations)
    {
        return commendations
            .Select(c => new Commendation(c.GiverName, c.ReceiverName, c.Name, c.Text, c.Type, c.RoundId))
            .ToList();
    }

    private void UpdateCommendations(NetUserId user, Commendation commendation, bool received)
    {
        if (!_commendations.TryGetValue(user, out var giverCommendations))
            return;

        var collection = received ? giverCommendations.Received : giverCommendations.Given;
        collection.Add(commendation);

        if (_player.TryGetSessionById(user, out var giverSession))
            SendCommendations(giverSession);
    }
}
