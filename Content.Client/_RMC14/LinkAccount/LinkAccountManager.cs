using Content.Shared._RMC14.LinkAccount;
using Robust.Shared.Network;

namespace Content.Client._RMC14.LinkAccount;

public sealed class LinkAccountManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;

    private readonly List<SharedRMCPatron> _allPatrons = [];

    public SharedRMCPatronTier? Tier { get; private set; }
    public bool Linked { get; private set; }
    public Color? GhostColor { get; private set; }
    public SharedRMCLobbyMessage? LobbyMessage { get; private set; }
    public SharedRMCRoundEndShoutouts? RoundEndShoutout { get; private set; }

    public event Action<Guid>? CodeReceived;
    public event Action? Updated;

    private void OnCode(LinkAccountCodeMsg message)
    {
        CodeReceived?.Invoke(message.Code);
    }

    private void OnStatus(LinkAccountStatusMsg ev)
    {
        Tier = ev.Patron?.Tier;
        Linked = ev.Patron?.Linked ?? false;
        GhostColor = ev.Patron?.GhostColor;
        LobbyMessage = ev.Patron?.LobbyMessage;
        RoundEndShoutout = ev.Patron?.RoundEndShoutout;
        Updated?.Invoke();
    }

    private void OnPatronList(RMCPatronListMsg ev)
    {
        _allPatrons.Clear();
        _allPatrons.AddRange(ev.Patrons);
    }

    public IReadOnlyList<SharedRMCPatron> GetPatrons()
    {
        return _allPatrons;
    }

    public bool CanViewPatronPerks()
    {
        return Tier is { } tier && (tier.GhostColor || tier.NamedItems || tier.Figurines || tier.LobbyMessage || tier.RoundEndShoutout);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountCodeMsg>(OnCode);
        _net.RegisterNetMessage<LinkAccountRequestMsg>();
        _net.RegisterNetMessage<LinkAccountStatusMsg>(OnStatus);
        _net.RegisterNetMessage<RMCPatronListMsg>(OnPatronList);
        _net.RegisterNetMessage<RMCClearGhostColorMsg>();
        _net.RegisterNetMessage<RMCChangeGhostColorMsg>();
        _net.RegisterNetMessage<RMCChangeLobbyMessageMsg>();
        _net.RegisterNetMessage<RMCChangeMarineShoutoutMsg>();
        _net.RegisterNetMessage<RMCChangeXenoShoutoutMsg>();
    }
}
