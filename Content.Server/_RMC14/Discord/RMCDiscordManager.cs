using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RMC14.CCVar;
using Discord;
using Discord.WebSocket;
using Robust.Shared.Configuration;
using LogMessage = Discord.LogMessage;

namespace Content.Server._RMC14.Discord;

public sealed class RMCDiscordManager : IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private DiscordSocketClient _client = default!;
    private ulong _adminChannelId;
    private ulong _mentorChannelId;
    private ITextChannel? _adminChannel;
    private ITextChannel? _mentorChannel;

    private ISawmill _sawmill = default!;
    private Task _discordThread = default!;

    private readonly ConcurrentQueue<RMCDiscordMessage> _chatMessages = new();
    private readonly ConcurrentQueue<RMCDiscordMessage> _discordMessages = new();

    private bool _running = true;
    private int _ready;

    private async void Initialize()
    {
        _sawmill = _logManager.GetSawmill("rmc.discord");

        try
        {
            var token = _config.GetCVar(RMCCVars.RMCDiscordToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                _sawmill.Info($"CVar {RMCCVars.RMCDiscordToken.Name} has no value. Disabling Discord bot.");
                return;
            }

            _discordThread = Task.Run(async () => await DiscordThreadMain());
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error occurred starting Discord bot:\n{e}");
        }
    }

    private async Task Log(LogMessage msg)
    {
        var severity = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Fatal,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Info,
            LogSeverity.Verbose => LogLevel.Verbose,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Info,
        };

        if (msg.Exception != null)
            _sawmill.Error($"{msg.Message}\n{msg.Exception}");
        else
            _sawmill.Log(severity, msg.Message);
    }

    public void Shutdown()
    {
        _running = false;
    }

    private async Task DiscordThreadMain()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
        });
        _client.Log += Log;
        _client.MessageReceived += OnDiscordMessageReceived;
        _client.Ready += OnDiscordReady;

        await _client.LoginAsync(TokenType.Bot, _config.GetCVar(RMCCVars.RMCDiscordToken));
        await _client.StartAsync();

        _adminChannelId = (ulong) _config.GetCVar(RMCCVars.RMCDiscordAdminChatChannel);
        if (_adminChannelId != 0)
            _adminChannel = (ITextChannel) await _client.GetChannelAsync(_adminChannelId);

        _mentorChannelId = (ulong) _config.GetCVar(RMCCVars.RMCDiscordMentorChatChannel);
        if (_mentorChannelId != 0)
            _mentorChannel = (ITextChannel) await _client.GetChannelAsync(_mentorChannelId);

        while (_running)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _ready, 1, 1) == 1)
                {
                    while (_chatMessages.TryDequeue(out var msg))
                    {
                        switch (msg.Type)
                        {
                            case RMCDiscordMessageType.Admin:
                                if (_adminChannel != null)
                                    await _adminChannel.SendMessageAsync(FormatDiscordMessage(msg));

                                break;
                            case RMCDiscordMessageType.Mentor:
                                if (_mentorChannel != null)
                                    await _mentorChannel.SendMessageAsync(FormatDiscordMessage(msg));

                                break;
                        }
                    }
                }

                await Task.Delay(1000);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error occurred sending message to Discord:\n{e}");
            }
        }
    }

    private async Task OnDiscordMessageReceived(SocketMessage msg)
    {
        if (!_running)
            return;

        if (msg.Author.Id == _client.CurrentUser.Id)
            return;

        RMCDiscordMessageType? type = null;
        if (msg.Channel.Id == _adminChannelId)
            type = RMCDiscordMessageType.Admin;

        if (msg.Channel.Id == _mentorChannelId)
            type = RMCDiscordMessageType.Mentor;

        if (type == null)
            return;

        var message = new RMCDiscordMessage($"\\[D\\] {msg.Author.Username}", msg.Content, type.Value);
        _discordMessages.Enqueue(message);
    }

    private async Task OnDiscordReady()
    {
        Interlocked.Exchange(ref _ready, 1);
    }

    public ConcurrentQueue<RMCDiscordMessage> GetDiscordMessages()
    {
        return _discordMessages;
    }

    public void SendDiscordAdminMessage(string author, string message)
    {
        _chatMessages.Enqueue(new RMCDiscordMessage(author, message, RMCDiscordMessageType.Admin));
    }

    public void SendDiscordMentorMessage(string author, string message)
    {
        _chatMessages.Enqueue(new RMCDiscordMessage(author, message, RMCDiscordMessageType.Mentor));
    }

    private string FormatDiscordMessage(RMCDiscordMessage msg)
    {
        return $"**{msg.Author}:** {msg.Message}";
    }

    public async Task Restart()
    {
        await _client.StopAsync();
        await _client.DisposeAsync();
        _ready = 0;
        _running = true;
        await _discordThread;
        Initialize();
    }

    public void PostInject()
    {
        Initialize();
    }
}
