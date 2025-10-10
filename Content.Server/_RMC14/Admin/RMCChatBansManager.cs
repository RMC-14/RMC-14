using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Admin.ChatBans;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Admin;

public sealed class RMCChatBansManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private const int Ipv4_CIDR = 32;
    private const int Ipv6_CIDR = 64;

    private ISawmill _sawmill = default!;
    private readonly Dictionary<NetUserId, List<ChatBan>> _bans = new();

    private async Task LoadData(ICommonSession player, CancellationToken cancel)
    {
        await ReloadBans(player.UserId);
    }

    private void ClientDisconnected(ICommonSession player)
    {
        _bans.Remove(player.UserId);
    }

    private ChatBan ConvertBan(RMCChatBans b)
    {
        return new ChatBan(b.Id, b.Type, b.BannedAt, b.ExpiresAt, b.UnbannedAt, b.UnbanningAdmin?.LastSeenUserName, b.Reason);
    }

    private async Task ReloadBans(NetUserId player)
    {
        var bans = await _db.GetActiveChatBans(player);
        _bans[player] = bans.Select(ConvertBan).ToList();
    }

    public async void AddChatBan(
        NetUserId target,
        TimeSpan? duration,
        ChatType type,
        NetUserId admin,
        string reason)
    {
        try
        {
            var data = await _playerLocator.LookupIdAsync(target);
            var address = data?.LastAddress;
            (IPAddress, int)? addressRange = null;
            if (address != null)
            {
                if (address.IsIPv4MappedToIPv6)
                    address = address.MapToIPv4();

                // Ban /64 for IPv6, /32 for IPv4.
                var hid = address.AddressFamily == AddressFamily.InterNetworkV6 ? Ipv6_CIDR : Ipv4_CIDR;
                addressRange = (address, hid);
            }

            var round = _entity.SystemOrNull<GameTicker>()?.RoundId;
            await _db.AddChatBan(round, target, addressRange, data?.LastHWId, duration, type, admin, reason);
            await ReloadBans(target);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error adding chat ban for player {target} with type {type} by admin {admin}:\n{e}");
        }
    }

    public async void TryPardonChatBan(int ban, NetUserId? admin)
    {
        try
        {
            var target = await _db.TryPardonChatBan(ban, admin);
            if (target != null)
                await ReloadBans(new NetUserId(target.Value));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error pardoning chat ban {ban} with admin {admin}:\n{e}");
        }
    }

    public bool IsChatBanned(NetUserId player, ChatType type)
    {
        if (!_bans.TryGetValue(player, out var bans))
            return false;

        foreach (var ban in bans)
        {
            if (ban.Type != type)
                continue;

            if (ban.ExpiresAt > DateTimeOffset.UtcNow.UtcDateTime)
                return true;
        }

        return false;
    }

    public async Task<List<ChatBan>> GetAllChatBans(NetUserId player)
    {
        var bans = await _db.GetAllChatBans(player);
        return bans.Select(ConvertBan).ToList();
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill("rmc.discord");
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
