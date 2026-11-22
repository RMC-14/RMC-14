using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.GMRequest;

/// <summary>
/// Handles giving out the GM Request verb and sending those requests to the manager.
/// </summary>
public sealed class GMRequestVerbSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly GMRequestManager _gmRequestManager = default!;

    //Tracks the next permitted time to send a request for a player
    //Handled here to avoid extra Shared systems, components, etc
    private Dictionary<ICommonSession, TimeSpan> _cooldowns = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddRequestVerb);
    }

    private void AddRequestVerb(GetVerbsEvent<Verb> args)
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();

        var requestEnabled = cfg.GetCVar(RMCCVars.RMCGMRequestEnabled);

        if (!requestEnabled)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        // The verb shows in the context menu of yourself
        if (args.User != args.Target)
            return;

        args.Verbs.Add(new()
            {
                Act = () =>
                {
                    _quickDialog.OpenDialog(actor.PlayerSession,
                        Loc.GetString("rmc-gm-request-verb-ui-title"),
                        Loc.GetString("rmc-gm-request-verb-ui-prompt"),
                        (string message) =>
                        {
                            // Make sure they still exist and that the CVar hasn't been disabled while they were typing
                            if (actor?.PlayerSession != null && requestEnabled)
                                SendGMRequest(actor.PlayerSession, message);
                        });
                },
                Text = Loc.GetString("rmc-gm-request-verb"),
                Priority = -30,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/group.svg.192dpi.png")),
            }
        );
    }

    /// <summary>
    /// Sends a message and username to the GMRequest Panel, as well as admin logs. Alerts admins with a chat message and a sound.
    /// </summary>
    /// <param name="sender">The session sending the message</param>
    /// <param name="message">The message being sent</param>
    /// <remarks>
    /// Essentially a tweaked version of the prayer system.
    /// </remarks>
    private void SendGMRequest(ICommonSession sender, string message)
    {
        if (sender.AttachedEntity == null)
            return;

        if (_cooldowns.TryGetValue(sender, out var time) && time > _timing.CurTime)
        {
            var delay = (int)Math.Ceiling((time -  _timing.CurTime).TotalSeconds);
            _popupSystem.PopupEntity(Loc.GetString("rmc-gm-request-verb-cooldown", ("secs", delay)), sender.AttachedEntity.Value, sender, PopupType.SmallCaution);
            return;
        }

        _cooldowns[sender] =  _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(RMCCVars.RMCGMRequestCooldownSeconds));

        _popupSystem.PopupEntity(Loc.GetString("rmc-gm-request-verb-sent"), sender.AttachedEntity.Value, sender, PopupType.Medium);

        _adminLogger.Add(LogType.AdminMessage, LogImpact.Low, $"{ToPrettyString(sender.AttachedEntity.Value):player} sent request: {message}");
        _gmRequestManager.Add(sender, message);
        //Admin chat announce is handled in the manager, to ensure the log was actually added before announcing it
    }
}
