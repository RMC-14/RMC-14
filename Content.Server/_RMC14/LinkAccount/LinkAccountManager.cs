using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.LinkAccount;
using Content.Shared._RMC14.Patron;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.LinkAccount;

public sealed class LinkAccountManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _lastRequest = new();
    private readonly TimeSpan _minimumWait = TimeSpan.FromSeconds(0.5);
    private readonly Dictionary<NetUserId, (SharedRMCPatronTier? Tier, bool Linked)> _connected = new();
    private readonly List<SharedRMCPatron> _allPatrons = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var tier = await _db.GetPatronTier(player.UserId, cancel);
        var linked = await _db.HasLinkedAccount(player.UserId, cancel);
        cancel.ThrowIfCancellationRequested();

        var sharedTier = tier == null
            ? null
            : new SharedRMCPatronTier(
                tier.ShowOnCredits,
                tier.NamedItems,
                tier.Figurines,
                tier.LobbyMessage,
                tier.RoundEndShoutout,
                tier.Name
            );

        _connected[player.UserId] = (sharedTier, linked);
    }

    private void FinishLoad(ICommonSession player)
    {
        SendPatronStatus(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _connected.Remove(player.UserId);
    }

    private void SendPatronStatus(ICommonSession player)
    {
        var connected = _connected.GetValueOrDefault(player.UserId);
        var msg = new LinkAccountStatusMsg
        {
            PatronTier = connected.Tier,
            Linked = connected.Linked,
        };
        _net.ServerSendMessage(msg, player.Channel);
        SendPatrons(player);
    }

    private void OnRequest(LinkAccountRequestMsg message)
    {
        var user = message.MsgChannel.UserId;
        var time = _timing.RealTime;
        if (_lastRequest.TryGetValue(user, out var last) &&
            last + _minimumWait > time)
        {
            return;
        }

        _lastRequest[user] = time;

        var code = Guid.NewGuid();
        _db.SetLinkingCode(user, code);

        var response = new LinkAccountCodeMsg { Code = code };
        _net.ServerSendMessage(response, message.MsgChannel);
    }

    public async Task RefreshAllPatrons()
    {
        var patrons = await _db.GetAllPatrons();

        _allPatrons.Clear();
        foreach (var patron in patrons)
        {
            _allPatrons.Add(new SharedRMCPatron(patron.Player.LastSeenUserName, patron.Tier.Name));
        }
    }

    public void SendPatronsToAll()
    {
        var msg = new RMCPatronListMsg { Patrons = _allPatrons };
        _net.ServerSendToAll(msg);
    }

    public void SendPatrons(ICommonSession player)
    {
        var msg = new RMCPatronListMsg { Patrons = _allPatrons };
        _net.ServerSendMessage(msg, player.Channel);
    }

    public SharedRMCPatronTier? GetPatronTier(ICommonSession player)
    {
        return _connected.GetValueOrDefault(player.UserId).Tier;
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountRequestMsg>(OnRequest);
        _net.RegisterNetMessage<LinkAccountCodeMsg>();
        _net.RegisterNetMessage<LinkAccountStatusMsg>();
        _net.RegisterNetMessage<RMCPatronListMsg>();
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
