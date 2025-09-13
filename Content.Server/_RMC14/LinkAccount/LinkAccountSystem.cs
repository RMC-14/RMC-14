using Content.Server._RMC14.Rules;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.GhostColor;
using Content.Shared._RMC14.LinkAccount;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.LinkAccount;

public sealed class LinkAccountSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _timeBetweenLobbyMessages;
    private TimeSpan _nextLobbyMessageTime;
    private TimeSpan _lobbyMessageInitialDelay;
    private (string Message, string User)? _nextLobbyMessage;
    private RoundEndShoutout? _nextMarineShoutout;
    private RoundEndShoutout? _nextXenoShoutout;

    public override void Initialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend, after: [typeof(CMDistressSignalRuleSystem)]);

        SubscribeLocalEvent<GhostColorComponent, PlayerAttachedEvent>(OnGhostColorPlayerAttached);

        SubscribeLocalEvent<PatronCustomNameComponent, MapInitEvent>(OnPatronCustomNameMapInit);

        Subs.CVar(_config, RMCCVars.RMCPatronLobbyMessageTimeSeconds, v => _timeBetweenLobbyMessages = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCPatronLobbyMessageInitialDelaySeconds, v => _lobbyMessageInitialDelay = TimeSpan.FromSeconds(v), true);

        ReloadPatrons();
        GetRandomLobbyMessage();
        GetRandomShoutout();

        _linkAccount.PatronUpdated += OnPatronUpdated;
    }

    public override void Shutdown()
    {
        _linkAccount.PatronUpdated -= OnPatronUpdated;
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.InRound:
                GetRandomShoutout();
                break;
            case GameRunLevel.PreRoundLobby:
            case GameRunLevel.PostRound:
                ReloadPatrons();
                GetRandomLobbyMessage();
                GetRandomShoutout();
                break;
        }

        if (ev.New == GameRunLevel.PreRoundLobby)
            _nextLobbyMessageTime = _timing.RealTime + _lobbyMessageInitialDelay;
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        if (_nextMarineShoutout is { } nextMarine)
        {
            ev.AddLine("\n");
            ev.AddLine(Loc.GetString("rmc-ui-shoutout-marine", ("name", nextMarine.Name)));
            _adminLog.Add(LogType.RMCRoundEnd, $"Showing round end shoutout from {nextMarine.Author:player}: {nextMarine.Name:text}");
        }

        if (_nextXenoShoutout is { } nextXeno)
        {
            ev.AddLine("\n");
            ev.AddLine(Loc.GetString("rmc-ui-shoutout-xeno", ("name", nextXeno.Name)));
            _adminLog.Add(LogType.RMCRoundEnd, $"Showing round end shoutout from {nextXeno.Author:player}: {nextXeno.Name:text}");
        }
    }

    private void OnGhostColorPlayerAttached(Entity<GhostColorComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!TryComp(ent, out ActorComponent? actor) ||
            _linkAccount.GetConnectedPatron(actor.PlayerSession.UserId) is not { } patron ||
            patron.Tier is not { GhostColor: true } ||
            patron.GhostColor is not { } color)
        {
            RemCompDeferred<GhostColorComponent>(ent);
            return;
        }

        ent.Comp.Color = color;
        Dirty(ent);
    }

    private void OnPatronCustomNameMapInit(Entity<PatronCustomNameComponent> ent, ref MapInitEvent args)
    {
        if (!_linkAccount.TryGetPatron(ent.Comp.User, out var patron))
            return;

        if (ent.Comp.Tier is { } tier && patron.Tier != tier)
            return;

        if (ent.Comp.Name is { } name)
            _metaData.SetEntityName(ent, name);

        if (ent.Comp.Description is { } description)
        {
            if (TryComp(ent, out MetaDataComponent? metaData))
                description = $"{metaData.EntityDescription}\n\n{description}";

            _metaData.SetEntityDescription(ent, description);
        }
    }

    private async void ReloadPatrons()
    {
        try
        {
            await _linkAccount.RefreshAllPatrons();
            _linkAccount.SendPatronsToAll();
        }
        catch (Exception e)
        {
            Log.Error($"Error reloading Patrons list:\n{e}");
        }
    }

    private async void GetRandomLobbyMessage()
    {
        try
        {
            _nextLobbyMessage = await _db.GetRandomLobbyMessage();
        }
        catch (Exception e)
        {
            Log.Error($"Error getting random lobby message:\n{e}");
        }
    }

    private async void GetRandomShoutout()
    {
        try
        {
            (_nextMarineShoutout, _nextXenoShoutout) = await _db.GetRandomShoutout();
        }
        catch (Exception e)
        {
            Log.Error($"Error getting random shoutout:\n{e}");
        }
    }

    private void OnPatronUpdated((NetUserId Id, SharedRMCPatronFull Patron) tuple)
    {
        if (_player.TryGetSessionById(tuple.Id, out var session) &&
            session.AttachedEntity is { } ent &&
            HasComp<GhostComponent>(ent))
        {
            var color = EnsureComp<GhostColorComponent>(ent);
            color.Color = tuple.Patron.GhostColor;
            Dirty(ent, color);
        }
    }

    public override void Update(float frameTime)
    {
        var time = _timing.RealTime;
        if (time < _nextLobbyMessageTime)
            return;

        _nextLobbyMessageTime = time + _timeBetweenLobbyMessages;

        if (_nextLobbyMessage is { } message)
        {
            _adminLog.Add(LogType.RMCLobbyMessage, $"Displaying lobby message from {message.User:user}: {message.Message:message}");
            RaiseNetworkEvent(new SharedRMCDisplayLobbyMessageEvent(message.Message, message.User));
        }

        GetRandomLobbyMessage();
    }
}
