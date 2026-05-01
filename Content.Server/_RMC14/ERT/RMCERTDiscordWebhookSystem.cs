using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Shared._RMC14.ERT;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Relays RMC ERT request state to Discord through the existing ahelp webhook.
/// </summary>
public sealed class RMCERTDiscordWebhookSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private const int EmbedTitleMax = 256;
    private const int EmbedFieldNameMax = 256;
    private const int EmbedFieldValueMax = 1024;
    private const int ServerNameMax = 1500;

    private readonly object _sync = new();
    private readonly Dictionary<Guid, WebhookRequestState> _requests = new();

    private string _webhookUrl = string.Empty;
    private WebhookIdentifier? _webhookIdentifier;
    private string _footerIconUrl = string.Empty;
    private string _avatarUrl = string.Empty;
    private string _serverName = string.Empty;
    private int _webhookGeneration;

    public override void Initialize()
    {
        Subs.CVar(_config, CCVars.DiscordAHelpWebhook, OnWebhookChanged, true);
        Subs.CVar(_config, CCVars.DiscordAHelpFooterIcon, value => _footerIconUrl = value, true);
        Subs.CVar(_config, CCVars.DiscordAHelpAvatar, value => _avatarUrl = value, true);
        Subs.CVar(_config, CVars.GameHostName, value => _serverName = value, true);
    }

    public void SyncRequest(RMCERTRequest request)
    {
        var payload = BuildPayload(request);

        lock (_sync)
        {
            if (!_requests.TryGetValue(request.Id, out var state))
            {
                state = new WebhookRequestState();
                _requests[request.Id] = state;
            }

            state.Payload = payload;
            state.Dirty = true;

            if (state.Processing || _webhookIdentifier == null)
                return;

            state.Processing = true;
        }

        ProcessRequest(request.Id);
    }

    public void ClearRequests()
    {
        lock (_sync)
        {
            _requests.Clear();
        }
    }

    private async void OnWebhookChanged(string url)
    {
        lock (_sync)
        {
            _webhookUrl = url;
            _webhookIdentifier = null;
            _webhookGeneration++;

            foreach (var state in _requests.Values)
                state.MessageId = 0;
        }

        if (string.IsNullOrWhiteSpace(url))
            return;

        if (await _discord.GetWebhook(url) is not { } data)
            return;

        lock (_sync)
        {
            if (_webhookUrl != url)
                return;

            _webhookIdentifier = data.ToIdentifier();
        }

        StartPendingUpdates();
    }

    private void StartPendingUpdates()
    {
        List<Guid> pending = [];

        lock (_sync)
        {
            if (_webhookIdentifier == null)
                return;

            foreach (var (id, state) in _requests)
            {
                if (state.Processing)
                    continue;

                state.Processing = true;
                pending.Add(id);
            }
        }

        foreach (var id in pending)
            ProcessRequest(id);
    }

    private async void ProcessRequest(Guid id)
    {
        while (true)
        {
            WebhookPayload payload;
            WebhookIdentifier identifier;
            ulong messageId;
            int generation;

            lock (_sync)
            {
                if (!_requests.TryGetValue(id, out var state))
                    return;

                if (_webhookIdentifier is not { } currentIdentifier)
                {
                    state.Processing = false;
                    return;
                }

                payload = state.Payload;
                identifier = currentIdentifier;
                messageId = state.MessageId;
                generation = _webhookGeneration;
                state.Dirty = false;
            }

            var success = false;
            ulong createdMessageId = 0;

            try
            {
                if (messageId == 0)
                {
                    var response = await _discord.CreateMessage(identifier, payload);
                    if (response.IsSuccessStatusCode)
                    {
                        createdMessageId = await GetMessageId(response);
                        success = createdMessageId != 0;
                        if (!success)
                            Log.Warning($"Discord ahelp webhook created ERT request {id} message without a parseable message id.");
                    }
                }
                else
                {
                    var response = await _discord.EditMessage(identifier, messageId, payload);
                    success = response.IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while syncing ERT request {id} to Discord ahelp webhook:\n{e}");
            }

            lock (_sync)
            {
                if (!_requests.TryGetValue(id, out var state))
                    return;

                if (generation != _webhookGeneration)
                {
                    state.Dirty = true;
                    continue;
                }

                if (createdMessageId != 0 && state.MessageId == 0)
                    state.MessageId = createdMessageId;

                if (!success || !state.Dirty)
                {
                    state.Processing = false;
                    return;
                }
            }
        }
    }

    private static async Task<ulong> GetMessageId(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var id = JsonNode.Parse(content)?["id"]?.GetValue<string>();
        return ulong.TryParse(id, out var messageId) ? messageId : 0;
    }

    private WebhookPayload BuildPayload(RMCERTRequest request)
    {
        var state = RMCERTLoc.GetState(request.State);
        var embed = new WebhookEmbed
        {
            Title = Clip(Loc.GetString("rmc-ert-discord-title",
                ("id", GetShortId(request.Id)),
                ("state", state)), EmbedTitleMax),
            Color = GetColor(request.State),
            Fields = BuildFields(request),
            Footer = new WebhookEmbedFooter
            {
                Text = GetFooterText(),
                IconUrl = string.IsNullOrWhiteSpace(_footerIconUrl) ? null : _footerIconUrl,
            },
        };

        return new WebhookPayload
        {
            Username = Loc.GetString("rmc-ert-discord-username"),
            AvatarUrl = string.IsNullOrWhiteSpace(_avatarUrl) ? null : _avatarUrl,
            Embeds = [embed],
            AllowedMentions = new WebhookMentions(),
        };
    }

    private List<WebhookEmbedField> BuildFields(RMCERTRequest request)
    {
        List<WebhookEmbedField> fields = [];
        AddField(fields, Loc.GetString("rmc-ert-discord-field-request-id"), request.Id.ToString());
        AddField(fields, Loc.GetString("rmc-ert-discord-field-state"), RMCERTLoc.GetState(request.State));
        AddField(fields, Loc.GetString("rmc-ert-discord-field-requester"), request.RequesterName);
        AddField(fields, Loc.GetString("rmc-ert-discord-field-source"), RMCERTLoc.GetSource(request.Source));
        AddField(fields, Loc.GetString("rmc-ert-discord-field-via"), request.SourceName);
        AddField(fields, Loc.GetString("rmc-ert-discord-field-created"), FormatRoundTime(request.CreatedAt));
        AddField(fields, Loc.GetString("rmc-ert-discord-field-reason"), request.Reason, false, 900);
        AddField(fields, Loc.GetString("rmc-ert-discord-field-allowed-calls"), FormatAllowedCalls(request), false, 900);

        if (request.SelectedCall is { } selectedCall)
            AddField(fields, Loc.GetString("rmc-ert-discord-field-selected-call"), FormatCall(selectedCall));

        var timing = FormatTiming(request);
        if (!string.IsNullOrWhiteSpace(timing))
            AddField(fields, Loc.GetString("rmc-ert-discord-field-timing"), timing, false);

        if (!string.IsNullOrWhiteSpace(request.LastWarning))
            AddField(fields, Loc.GetString("rmc-ert-discord-field-warning"), request.LastWarning, false, 900);

        if (!string.IsNullOrWhiteSpace(request.LastError))
            AddField(fields, Loc.GetString("rmc-ert-discord-field-error"), request.LastError, false, 900);

        return fields;
    }

    private void AddField(
        List<WebhookEmbedField> fields,
        string name,
        string? value,
        bool inline = true,
        int maxValueLength = EmbedFieldValueMax)
    {
        fields.Add(new WebhookEmbedField
        {
            Name = Clip(name, EmbedFieldNameMax),
            Value = FormatFieldValue(value, maxValueLength),
            Inline = inline,
        });
    }

    private string FormatAllowedCalls(RMCERTRequest request)
    {
        if (request.AllowedCalls.Count == 0)
            return Loc.GetString("rmc-ert-discord-empty");

        return string.Join('\n', request.AllowedCalls.Select(FormatCall));
    }

    private string FormatCall(ProtoId<RMCERTCallPrototype> callId)
    {
        return _prototypes.TryIndex(callId, out var call)
            ? call.Name
            : callId.Id;
    }

    private string FormatTiming(RMCERTRequest request)
    {
        List<string> timing = [];

        if (request.DispatchAt is { } dispatchAt)
            timing.Add(Loc.GetString("rmc-ert-discord-timing-dispatch",
                ("time", FormatRoundTime(dispatchAt))));

        if (request.RecruitmentEndsAt is { } recruitmentEndsAt)
            timing.Add(Loc.GetString("rmc-ert-discord-timing-recruitment-ends",
                ("time", FormatRoundTime(recruitmentEndsAt))));

        return string.Join('\n', timing);
    }

    private string GetFooterText()
    {
        var serverName = Clip(_serverName, ServerNameMax);
        var round = _gameTicker.RunLevel switch
        {
            GameRunLevel.PreRoundLobby => _gameTicker.RoundId == 0
                ? Loc.GetString("rmc-ert-discord-round-pre-restart")
                : Loc.GetString("rmc-ert-discord-round-pre",
                    ("round", _gameTicker.RoundId + 1)),
            GameRunLevel.InRound => Loc.GetString("rmc-ert-discord-round-in",
                ("round", _gameTicker.RoundId)),
            GameRunLevel.PostRound => Loc.GetString("rmc-ert-discord-round-post",
                ("round", _gameTicker.RoundId)),
            _ => _gameTicker.RunLevel.ToString(),
        };

        return Loc.GetString("rmc-ert-discord-footer",
            ("server", serverName),
            ("round", round));
    }

    private string FormatFieldValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Loc.GetString("rmc-ert-discord-empty");

        var sanitized = SanitizeDiscord(value.Trim());
        return string.IsNullOrWhiteSpace(sanitized)
            ? Loc.GetString("rmc-ert-discord-empty")
            : Clip(sanitized, maxLength);
    }
    private static string SanitizeDiscord(string value)
    {
        return FormattedMessage.RemoveMarkupPermissive(value)
            .Replace("@", "@\u200B")
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
    }

    private static string Clip(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        if (maxLength <= 3)
            return value[..maxLength];

        return $"{value[..(maxLength - 3)]}...";
    }

    private static string GetShortId(Guid id)
    {
        return id.ToString()[..8];
    }

    private static int GetColor(RMCERTRequestState state)
    {
        return state switch
        {
            RMCERTRequestState.PendingAdmin or RMCERTRequestState.Requested => 0xF2C94C,
            RMCERTRequestState.PendingDispatch or RMCERTRequestState.Recruiting or
                RMCERTRequestState.Spawning or RMCERTRequestState.Launching => 0x3498DB,
            RMCERTRequestState.Arrived or RMCERTRequestState.Completed => 0x2ECC71,
            RMCERTRequestState.Denied or RMCERTRequestState.Failed => 0xE74C3C,
            RMCERTRequestState.Cancelled => 0x95A5A6,
            _ => 0x95A5A6,
        };
    }

    private static string FormatRoundTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    private sealed class WebhookRequestState
    {
        public WebhookPayload Payload;
        public ulong MessageId;
        public bool Processing;
        public bool Dirty;
    }
}
