using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Network;

namespace Content.Client._RMC14.Xenonids;

/// <summary>
/// Receives and caches the local player's total successful parasite infections, sent by the server.
/// </summary>
public sealed class XenoInfectionsManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;

    private int _infections;

    public event Action? Updated;

    public int Infections => _infections;

    private void OnInfections(RMCParasiteInfectionsMsg message)
    {
        _infections = message.Infections;
        Updated?.Invoke();
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<RMCParasiteInfectionsMsg>(OnInfections);
    }
}
