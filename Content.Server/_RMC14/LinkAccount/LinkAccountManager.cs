using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.LinkAccount;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Color = System.Drawing.Color;

namespace Content.Server._RMC14.LinkAccount;

public sealed class LinkAccountManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _lastRequest = new();
    private readonly TimeSpan _minimumWait = TimeSpan.FromSeconds(0.5);
    private readonly Dictionary<NetUserId, SharedRMCPatronFull> _connected = new();
    private readonly Dictionary<NetUserId, SharedRMCPatron> _allPatrons = [];
    private readonly HashSet<Guid> _figurines = [];

    public event Action? PatronsReloaded;
    public event Action<(NetUserId Id, SharedRMCPatronFull Patron)>? PatronUpdated;

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var patron = await _db.GetPatron(player.UserId, cancel);
        var linked = await _db.HasLinkedAccount(player.UserId, cancel);
        cancel.ThrowIfCancellationRequested();

        var tier = patron?.Tier;
        var sharedTier = tier == null
            ? null
            : new SharedRMCPatronTier(
                tier.ShowOnCredits,
                tier.GhostColor,
                tier.NamedItems,
                tier.Figurines,
                tier.LobbyMessage,
                tier.RoundEndShoutout,
                tier.Name
            );

        SharedRMCLobbyMessage? lobbyMessage = null;
        if (patron?.LobbyMessage is { Message.Length: > 0 } patronMsg)
            lobbyMessage = new SharedRMCLobbyMessage(patronMsg.Message);

        var marineName = patron?.RoundEndMarineShoutout?.Name;
        var xenoName = patron?.RoundEndXenoShoutout?.Name;
        SharedRMCRoundEndShoutouts? shoutouts = null;
        if (marineName != null || xenoName != null)
            shoutouts = new SharedRMCRoundEndShoutouts(marineName, xenoName);

        Robust.Shared.Maths.Color? ghostColor = null;
        if (patron?.GhostColor is { } patronColor)
        {
            var sysColor = Color.FromArgb(patronColor);
            ghostColor = new Robust.Shared.Maths.Color(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
        }

        _connected[player.UserId] = new SharedRMCPatronFull(sharedTier, linked, ghostColor, lobbyMessage, shoutouts);
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
        var msg = new LinkAccountStatusMsg { Patron = connected, };
        _net.ServerSendMessage(msg, player.Channel);
        SendPatrons(player);
    }

    private void SendPatronStatus(NetUserId user)
    {
        if (_player.TryGetSessionById(user, out var session))
            SendPatronStatus(session);
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

    private void OnClearGhostColor(RMCClearGhostColorMsg message)
    {
        SetGhostColor(message.MsgChannel.UserId, null);
    }

    private void OnChangeGhostColor(RMCChangeGhostColorMsg message)
    {
        SetGhostColor(message.MsgChannel.UserId, message.Color);
    }

    private void OnChangeLobbyMessage(RMCChangeLobbyMessageMsg message)
    {
        var text = message.Text;
        if (text == null)
            return;

        var user = message.MsgChannel.UserId;
        if (GetConnectedPatron(user)?.Tier is not { LobbyMessage: true })
            return;

        if (text.Length > SharedRMCLobbyMessage.CharacterLimit)
            text = text[..SharedRMCLobbyMessage.CharacterLimit];

        _db.SetLobbyMessage(user, text);
        OnPatronUpdated(user, p => p with { LobbyMessage = new SharedRMCLobbyMessage(text) });
    }

    private void OnChangeMarineShoutout(RMCChangeMarineShoutoutMsg message)
    {
        var name = message.Name;
        if (name == null)
            return;

        var user = message.MsgChannel.UserId;
        if (GetConnectedPatron(user)?.Tier is not { RoundEndShoutout: true })
            return;

        if (name.Length > SharedRMCRoundEndShoutouts.CharacterLimit)
            name = name[..SharedRMCRoundEndShoutouts.CharacterLimit];

        _db.SetMarineShoutout(user, name);
        OnPatronUpdated(user, p => p with { RoundEndShoutout = new SharedRMCRoundEndShoutouts(name, p.RoundEndShoutout?.Xeno) });
    }

    private void OnChangeXenoShoutout(RMCChangeXenoShoutoutMsg message)
    {
        var name = message.Name;
        if (name == null)
            return;

        var user = message.MsgChannel.UserId;
        if (GetConnectedPatron(user)?.Tier is not { RoundEndShoutout: true })
            return;

        if (name.Length > SharedRMCRoundEndShoutouts.CharacterLimit)
            name = name[..SharedRMCRoundEndShoutouts.CharacterLimit];

        _db.SetXenoShoutout(user, name);
        OnPatronUpdated(user, p => p with { RoundEndShoutout = new SharedRMCRoundEndShoutouts(p.RoundEndShoutout?.Marine, name) });
    }

    private void SetGhostColor(NetUserId user, Robust.Shared.Maths.Color? color)
    {
        if (GetConnectedPatron(user)?.Tier is not { GhostColor: true })
            return;

        Color? sysColor = color == null ? null : Color.FromArgb(color.Value.ToArgb());
        _db.SetGhostColor(user, sysColor);
        OnPatronUpdated(user, p => p with { GhostColor = color });
    }

    private void OnPatronUpdated(NetUserId user, Func<SharedRMCPatronFull, SharedRMCPatronFull> action)
    {
        if (!_connected.TryGetValue(user, out var connected))
            return;

        connected = action(connected);
        _connected[user] = connected;
        PatronUpdated?.Invoke((user, connected));
        SendPatronStatus(user);
    }

    public async Task RefreshAllPatrons()
    {
        var patrons = await _db.GetAllPatrons();

        _allPatrons.Clear();
        _figurines.Clear();
        foreach (var patron in patrons)
        {
            _allPatrons[new NetUserId(patron.PlayerId)] = new SharedRMCPatron(patron.Player.LastSeenUserName, patron.Tier.Name);

            if (patron.Tier.Figurines)
                _figurines.Add(patron.PlayerId);
        }

        PatronsReloaded?.Invoke();
    }

    public void SendPatronsToAll()
    {
        var msg = new RMCPatronListMsg { Patrons = _allPatrons.Values.ToList() };
        _net.ServerSendToAll(msg);
    }

    private void SendPatrons(ICommonSession player)
    {
        var msg = new RMCPatronListMsg { Patrons = _allPatrons.Values.ToList() };
        _net.ServerSendMessage(msg, player.Channel);
    }

    public SharedRMCPatronFull? GetConnectedPatron(ICommonSession player)
    {
        return GetConnectedPatron(player.UserId);
    }

    public SharedRMCPatronFull? GetConnectedPatron(NetUserId userId)
    {
        return _connected.GetValueOrDefault(userId);
    }

    public bool TryGetPatron(NetUserId userId, [NotNullWhen(true)] out SharedRMCPatron? tier)
    {
        return _allPatrons.TryGetValue(userId, out tier);
    }

    public IReadOnlySet<Guid> GetFigurines()
    {
        return _figurines;
    }

    public string GetPatronOOCHexColor(NetUserId userId)
    {
        // TODO RMC14 move this to the database
        if (userId.UserId == Guid.Parse("518fe396-b199-4757-92ff-697fc527bbf1"))
            return "#FFFFFF";

        return "#aa00ff";
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountRequestMsg>(OnRequest);
        _net.RegisterNetMessage<LinkAccountCodeMsg>();
        _net.RegisterNetMessage<LinkAccountStatusMsg>();
        _net.RegisterNetMessage<RMCPatronListMsg>();
        _net.RegisterNetMessage<RMCClearGhostColorMsg>(OnClearGhostColor);
        _net.RegisterNetMessage<RMCChangeGhostColorMsg>(OnChangeGhostColor);
        _net.RegisterNetMessage<RMCChangeLobbyMessageMsg>(OnChangeLobbyMessage);
        _net.RegisterNetMessage<RMCChangeMarineShoutoutMsg>(OnChangeMarineShoutout);
        _net.RegisterNetMessage<RMCChangeXenoShoutoutMsg>(OnChangeXenoShoutout);
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
