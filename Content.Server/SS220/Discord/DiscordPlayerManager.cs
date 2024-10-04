// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Players;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Discord;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.Discord;

public sealed class DiscordPlayerManager : IPostInjectInit, IDisposable
{
    internal SponsorUsers? CachedSponsorUsers => _cachedSponsorUsers;

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;


    private ISawmill _sawmill = default!;
    private Timer? _statusRefreshTimer; // We should keep reference or else evil GC will kill our timer
    private volatile SponsorUsers? _cachedSponsorUsers;
    private readonly HttpClient _httpClient = new();

    private string _apiUrl = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("DiscordPlayerManager");

        _netMgr.RegisterNetMessage<MsgUpdatePlayerDiscordStatus>();

        _cfg.OnValueChanged(CCVars220.DiscordAuthApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCVars220.DiscordAuthApiKey, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", v);
        },
        true);

        _statusRefreshTimer = new Timer(async _ =>
        {
            _cachedSponsorUsers = await GetSponsorUsers();
        },
            state: null,
            dueTime: TimeSpan.FromSeconds(_cfg.GetCVar(CCVars220.DiscordSponsorsCacheLoadDelaySeconds)),
            period: TimeSpan.FromSeconds(_cfg.GetCVar(CCVars220.DiscordSponsorsCacheRefreshIntervalSeconds))
        );
    }

    void IPostInjectInit.PostInject()
    {
        _playerManager.PlayerStatusChanged += PlayerManager_PlayerStatusChanged;
    }

    public void Dispose()
    {
        _statusRefreshTimer?.Dispose();
        _httpClient.Dispose();
    }

    private async void PlayerManager_PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            await UpdateUserDiscordRolesStatus(e);
        }
    }

    private async Task UpdateUserDiscordRolesStatus(SessionStatusEventArgs e)
    {
        var info = await GetSponsorInfo(e.Session.UserId);

        if (info is not null)
        {
            _netMgr.ServerSendMessage(new MsgUpdatePlayerDiscordStatus
            {
                Info = info
            },
            e.Session.Channel);

            // Cache info in content data
            var contentPlayerData = e.Session.ContentData();
            if (contentPlayerData == null)
                return;

            contentPlayerData.SponsorInfo = info;
        }
    }

    private async Task<DiscordSponsorInfo?> GetSponsorInfo(NetUserId userId)
    {
        if (string.IsNullOrEmpty(_apiUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_apiUrl}/userinfo/{userId.UserId}";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get player sponsor info: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<DiscordSponsorInfo>(GetJsonSerializerOptions());
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return null;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var opt = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        opt.Converters.Add(new JsonStringEnumConverter());

        return opt;
    }

    /// <summary>
    /// Проверка, генерация ключа для дискорда.
    /// </summary>
    /// <param name="playerData"></param>
    /// <returns></returns>
    public async Task<string> CheckAndGenerateKey(SessionData playerData)
    {
        if (string.IsNullOrEmpty(_apiUrl))
        {
            return string.Empty;
        }

        try
        {
            var url = $"{_apiUrl}/userinfo/link/{playerData.UserId}";

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get player sponsor info: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return string.Empty;
            }

            return await response.Content.ReadFromJsonAsync<string>(GetJsonSerializerOptions()) ?? string.Empty;
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return string.Empty;
    }

    public async Task<PrimeListUserStatus?> GetUserPrimeListStatus(Guid userId)
    {
        if (string.IsNullOrEmpty(_apiUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_apiUrl}/checkPrimeAccess/{userId}";

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get user prime list status: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<PrimeListUserStatus>(GetJsonSerializerOptions());
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return null;
    }

    /// <summary>
    /// Возвращает список спонсоров проекта.
    /// </summary>
    /// <returns></returns>
    internal async Task<SponsorUsers?> GetSponsorUsers()
    {
        if (string.IsNullOrWhiteSpace(_apiUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_apiUrl}/userinfo/sponsors";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get sponsor users info: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<SponsorUsers>(GetJsonSerializerOptions());
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return null;
    }
}

