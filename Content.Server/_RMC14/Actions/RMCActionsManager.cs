using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Serilog;

namespace Content.Server._RMC14.Actions;

public sealed class RMCActionsManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly ITaskManager _task = default!;

    public event Action<ICommonSession, Dictionary<EntProtoId, ImmutableArray<EntProtoId>>?>? OnLoaded;

    private ISawmill _log = null!;
    private readonly Dictionary<NetUserId, Dictionary<EntProtoId, ImmutableArray<EntProtoId>>> _actionOrders = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        // TODO RMC14 read the migration.yml file to map old ids to new ones if necessary, otherwise ordering data is lost
        var orders = await _db.GetAllActionOrders(player.UserId);
        orders ??= new Dictionary<string, List<string>>();

        _actionOrders[player.UserId] = orders.ToDictionary(
            kvp => new EntProtoId(kvp.Key),
            kvp => kvp.Value.Select(s => new EntProtoId(s)).ToImmutableArray()
        );

        _task.RunOnMainThread(() => OnLoaded?.Invoke(player, _actionOrders.GetValueOrDefault(player.UserId)));
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _actionOrders.Remove(player.UserId);
    }

    public ImmutableArray<EntProtoId>? GetOrder(NetUserId player, EntProtoId id)
    {
        return _actionOrders.GetValueOrDefault(player)?.GetValueOrDefault(id, ImmutableArray<EntProtoId>.Empty);
    }

    public async void SetOrder(NetUserId player, EntProtoId id, List<EntProtoId> actions)
    {
        try
        {
            _actionOrders.GetOrNew(player)[id] = actions.ToImmutableArray();
            await _db.SetActionOrder(player, id, actions.Select(a => a.Id).ToList());
        }
        catch (Exception e)
        {
            _log.Error($"Error setting order of actions for player {player} with id {id}:\n{e}");
        }
    }

    public void PostInject()
    {
        _log = _logManager.GetSawmill("rmc_actions");
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
