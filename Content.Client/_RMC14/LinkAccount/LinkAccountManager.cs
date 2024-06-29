using Content.Shared._RMC14.LinkAccount;
using Content.Shared._RMC14.Patron;
using Robust.Shared.Network;

namespace Content.Client._RMC14.LinkAccount;

public sealed class LinkAccountManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;

    private readonly List<SharedRMCPatron> _allPatrons = new();

    public SharedRMCPatronTier? Tier { get; private set; }
    public bool Linked { get; private set; }

    private void OnStatus(LinkAccountStatusMsg ev)
    {
        Tier = ev.PatronTier;
        Linked = ev.Linked;
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

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<LinkAccountRequestMsg>();
        _net.RegisterNetMessage<LinkAccountStatusMsg>(OnStatus);
        _net.RegisterNetMessage<RMCPatronListMsg>(OnPatronList);
    }
}
