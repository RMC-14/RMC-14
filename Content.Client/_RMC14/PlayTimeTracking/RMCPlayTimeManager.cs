using Content.Shared._RMC14.PlayTimeTracking;
using Robust.Shared.Network;

namespace Content.Client._RMC14.PlayTimeTracking;

public sealed class RMCPlayTimeManager : SharedRMCPlayTimeManager
{
    [Dependency] private readonly INetManager _net = default!;

    private readonly HashSet<string> _excluded = [];

    public event Action? Updated;

    protected override void PostInject()
    {
        base.PostInject();
        _net.RegisterNetMessage<RMCExcludedTimersMsg>(OnExcludedTimers);
    }

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
}
