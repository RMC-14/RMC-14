using Content.Server.Database;
using Content.Shared._RMC14.LinkAccount;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.LinkAccount;

public sealed class LinkAccountManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _lastRequest = new();

    private readonly TimeSpan _minimumWait = TimeSpan.FromSeconds(0.5);

    private void OnRequest(LinkAccountRequestEvent message)
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

        var response = new LinkAccountCodeEvent { Code = code };
        _net.ServerSendMessage(response, message.MsgChannel);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountRequestEvent>(OnRequest);
        _net.RegisterNetMessage<LinkAccountCodeEvent>();
    }
}
