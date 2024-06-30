using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RMC14.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.PlayTimeTracking;

public sealed class RMCPlayTimeManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, HashSet<string>> _excluded = [];

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        var excluded = await _db.GetExcludedRoleTimers(player.UserId, cancel);
        cancel.ThrowIfCancellationRequested();
        _excluded[player.UserId] = new HashSet<string>(excluded);
    }

    private void FinishLoad(ICommonSession player)
    {
        SendExcludedTimers(player);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _excluded.Remove(player.UserId);
    }

    private void SendExcludedTimers(ICommonSession player)
    {
        var msg = new RMCExcludedTimersMsg
        {
            Trackers = _excluded.GetValueOrDefault(player.UserId) ?? [],
        };
        _net.ServerSendMessage(msg, player.Channel);
    }

    public async Task<bool> Exclude(NetUserId player, string tracker)
    {
        var excluded = await _db.ExcludeRoleTimer(player, tracker);
        if (_excluded.TryGetValue(player, out var trackers))
            trackers.Add(tracker);

        if (excluded && _player.TryGetSessionById(player, out var session))
            SendExcludedTimers(session);

        return excluded;
    }

    public bool IsExcluded(ICommonSession player, string tracker)
    {
        return _excluded.GetValueOrDefault(player.UserId)?.Contains(tracker) ?? false;
    }

    public IReadOnlySet<string>? GetExcluded(NetUserId player)
    {
        return _excluded.GetValueOrDefault(player);
    }

    public void RemoveWhereExcluded(ICommonSession player, HashSet<ProtoId<JobPrototype>> trackers)
    {
        if (!_excluded.TryGetValue(player.UserId, out var excluded))
            return;

        foreach (var exclude in excluded)
        {
            trackers.Remove(exclude);
        }
    }

    public async Task<bool> RemoveRoleTimerExclusion(NetUserId player, string tracker)
    {
        if (!await _db.RemoveRoleTimerExclusion(player, tracker))
            return false;

        if (_excluded.TryGetValue(player, out var excluded) &&
            excluded.Remove(tracker) &&
            _player.TryGetSessionById(player, out var session))
        {
            SendExcludedTimers(session);
        }

        return true;
    }

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<RMCExcludedTimersMsg>();
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
