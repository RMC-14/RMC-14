using Content.Shared._RMC14.PlayTimeTracking;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._RMC14.PlayTimeTracking;

public sealed class RMCPlayTimeManager : IPostInjectInit
{
    [Dependency] private readonly INetManager _net = default!;

    private readonly HashSet<string> _excluded = [];

    public event Action? Updated;

    private void OnExcludedTimers(RMCExcludedTimersMsg message)
    {
        _excluded.Clear();
        _excluded.UnionWith(message.Trackers);
        Updated?.Invoke();
    }

    public bool IsExcluded(string tracker)
    {
        return _excluded.Contains(tracker);
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<RMCExcludedTimersMsg>(OnExcludedTimers);
    }
}
