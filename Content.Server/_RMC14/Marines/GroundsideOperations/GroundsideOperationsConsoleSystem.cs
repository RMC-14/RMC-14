using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.GroundsideOperations;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Marines.GroundsideOperations;

public sealed class GroundsideOperationsConsoleSystem : SharedGroundsideOperationsConsoleSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ARESCoreSystem _core = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private int _characterLimit = 1000;
    private TimeSpan _generalQuartersCooldown;
    private TimeSpan _nextGeneralQuarters;

    private static readonly EntProtoId<ARESLogTypeComponent> LogCat = "ARESTabAnnouncementLogs";

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, RMCCVars.RMCGroundsideOperationsGeneralQuartersCooldownMinutes,
            minutes => _generalQuartersCooldown = TimeSpan.FromMinutes(minutes), true);
        Subs.CVar(_config, Content.Shared.CCVar.CCVars.ChatMaxMessageLength, limit => _characterLimit = limit, true);
    }

    protected override void TrySendHighCommand(Entity<GroundsideOperationsConsoleComponent> ent, EntityUid actor, string message)
    {
        message = message.Trim();
        if (message.Length == 0)
            return;

        if (message.Length > _characterLimit)
            message = message[.._characterLimit];

        var time = _timing.CurTime;
        if (ent.Comp.LastHighCommand is { } last && time < last + ent.Comp.HighCommandCooldown)
        {
            _popup.PopupClient(Loc.GetString("rmc-goc-high-command-cooldown", ("seconds", (int) ent.Comp.HighCommandCooldown.TotalSeconds)), actor, PopupType.MediumCaution);
            return;
        }

        ent.Comp.LastHighCommand = time;
        Dirty(ent);

        var staffMessage = Loc.GetString("rmc-goc-high-command-admin-message", ("sender", Name(actor)), ("message", message));
        _chat.SendAdminAnnouncement(staffMessage);
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/attention_jingle.ogg"),
            Filter.Empty().AddPlayers(_adminManager.ActiveAdmins),
            false,
            AudioParams.Default.WithVolume(-8f));
        _adminLog.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(actor):player} sent USCM High Command message: {message}");
        _popup.PopupClient(Loc.GetString("rmc-goc-high-command-sent"), actor, PopupType.Medium);
    }

    protected override void TrySetRedAlert(Entity<GroundsideOperationsConsoleComponent> ent, EntityUid actor)
    {
        if (_alertLevel.Get() >= RMCAlertLevels.Red)
        {
            _popup.PopupClient(Loc.GetString("rmc-goc-red-alert-already-set"), actor, PopupType.MediumCaution);
            return;
        }

        _alertLevel.Set(RMCAlertLevels.Red, actor);
    }

    protected override void TryCallGeneralQuarters(Entity<GroundsideOperationsConsoleComponent> ent, EntityUid actor)
    {
        var time = _timing.CurTime;
        if (time < _nextGeneralQuarters)
        {
            var seconds = (int) (_nextGeneralQuarters - time).TotalSeconds;
            _popup.PopupClient(Loc.GetString("rmc-goc-general-quarters-cooldown", ("seconds", seconds)), actor, PopupType.MediumCaution);
            return;
        }

        _nextGeneralQuarters = time + _generalQuartersCooldown;
        SyncGeneralQuartersCooldown();

        if (_alertLevel.Get() is not { } alert || alert < RMCAlertLevels.Red)
            _alertLevel.Set(RMCAlertLevels.Red, actor, playSound: false, sendAnnouncement: false);

        var text = Loc.GetString("rmc-announcement-general-quarters");
        _marineAnnounce.AnnounceARES(ent.Owner, text,
            new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/GQfullcall.ogg"));
        _core.CreateARESLog(ent, LogCat, (string)$"{Name(actor)} called General Quarters");
        _adminLog.Add(LogType.RMCAlertLevel, $"{ToPrettyString(actor):player} called General Quarters from {ToPrettyString(ent.Owner):console}");
        _popup.PopupClient(Loc.GetString("rmc-goc-general-quarters-sent"), actor, PopupType.Medium);
    }

    protected override void OnMapInit(Entity<GroundsideOperationsConsoleComponent> ent, ref MapInitEvent args)
    {
        base.OnMapInit(ent, ref args);
        SyncGeneralQuartersCooldown(ent);
    }

    protected override void OnBoundUiOpened(Entity<GroundsideOperationsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        base.OnBoundUiOpened(ent, ref args);
        SyncGeneralQuartersCooldown(ent);
    }

    private void SyncGeneralQuartersCooldown()
    {
        var query = EntityQueryEnumerator<GroundsideOperationsConsoleComponent>();
        while (query.MoveNext(out var uid, out var groundside))
            SyncGeneralQuartersCooldown((uid, groundside));
    }

    private void SyncGeneralQuartersCooldown(Entity<GroundsideOperationsConsoleComponent> ent)
    {
        ent.Comp.NextGeneralQuarters = _nextGeneralQuarters;
        Dirty(ent);
    }
}
