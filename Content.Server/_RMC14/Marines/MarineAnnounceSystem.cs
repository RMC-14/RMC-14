using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Roles.Jobs;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Radio;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Marines;

public sealed class MarineAnnounceSystem : SharedMarineAnnounceSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private int _characterLimit = 1000;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key,
            subs =>
            {
                subs.Event<MarineCommunicationsComputerMsg>(OnMarineCommunicationsComputerMsg);
            });

        Subs.CVar(_config, CCVars.ChatMaxMessageLength, limit => _characterLimit = limit, true);
    }

    private void OnMarineCommunicationsComputerMsg(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsComputerMsg args)
    {
        _ui.CloseUi(ent.Owner, MarineCommunicationsComputerUI.Key);

        var time = _timing.CurTime;
        if (_timing.CurTime < ent.Comp.LastAnnouncement + ent.Comp.Cooldown)
        {
            // TODO RMC14 localize
            _popup.PopupClient($"Please allow at least {(int) ent.Comp.Cooldown.TotalSeconds} seconds to pass between announcements", args.Actor);
            return;
        }

        var text = args.Text;
        if (text.Length > _characterLimit)
            text = text[.._characterLimit].Trim();

        Announce(args.Actor, text, ent.Comp.Sound);

        ent.Comp.LastAnnouncement = time;
        Dirty(ent);
    }

    public void Announce(EntityUid sender, string message, SoundSpecifier sound)
    {
        // TODO RMC14 localize this
        // TODO RMC14 rank
        var job = string.Empty;
        if (_mind.TryGetMind(sender, out var mindId, out _) &&
            _job.MindTryGetJobName(mindId, out var jobName))
        {
            job = jobName;
        }

        var name = Name(sender);
        var wrappedMessage =
            $"[font size=14][bold][color=white]Command Announcement[/color][/bold][/font]\n[font size=12][color=red]\n{message}\n\nSigned by,\n{job} {name}[/color][/font]";

        // TODO RMC14 receivers
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(e =>
                HasComp<MarineComponent>(e) ||
                HasComp<GhostComponent>(e)
            );
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, default, false, true, null);
        _audio.PlayGlobal(sound, filter, true, AudioParams.Default.WithVolume(-2f));
        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced message: {message}");
    }

    public override void AnnounceRadio(EntityUid sender, string message, ProtoId<RadioChannelPrototype> channel)
    {
        base.AnnounceRadio(sender, message, channel);

        _adminLogs.Add(LogType.RMCMarineAnnounce, $"{ToPrettyString(sender):source} marine announced radio message: {message}");
        _radio.SendRadioMessage(sender, message, channel, sender);
    }
}
