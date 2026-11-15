using System.Text.RegularExpressions;
using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Psychic;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Xenonids.Psychic;

public sealed class XenoPsychicCommunicationSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedCMChatSystem _chat = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly SharedXenoWatchSystem _watch = default!;

    private static readonly Color PsychicColor = Color.FromHex("#921992");
    private static readonly Regex NewLineRegex = new("\n{3,}", RegexOptions.Compiled);

    private readonly HashSet<Entity<MobStateComponent>> _nearbyMobs = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoPsychicCommunicationComponent, XenoPsychicWhisperActionEvent>(OnWhisperAction);
        SubscribeLocalEvent<XenoPsychicCommunicationComponent, XenoPsychicWhisperInputEvent>(OnWhisperInput);
        SubscribeLocalEvent<XenoPsychicCommunicationComponent, XenoPsychicRadianceActionEvent>(OnRadianceAction);
        SubscribeLocalEvent<XenoPsychicCommunicationComponent, XenoPsychicRadianceInputEvent>(OnRadianceInput);
        SubscribeLocalEvent<XenoPsychicCommunicationComponent, XenoGiveOrderActionEvent>(OnGiveOrderAction);
        SubscribeLocalEvent<XenoPsychicCommunicationComponent, XenoGiveOrderInputEvent>(OnGiveOrderInput);
    }

    private void OnWhisperAction(Entity<XenoPsychicCommunicationComponent> queen, ref XenoPsychicWhisperActionEvent args)
    {
        if (args.Handled || !CanUseQueen(queen))
            return;

        if (!CanUseQueen(queen) ||
            !TryGetAction(queen, GetNetEntity(args.Action), out _) ||
            !CanWhisperTo(queen, args.Target))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-psychic-target-invalid"), queen, queen, PopupType.MediumCaution);
            return;
        }

        _dialog.OpenInput(
            queen,
            Loc.GetString("rmc-xeno-psychic-whisper-message", ("target", QueenTargetName(queen, args.Target))),
            new XenoPsychicWhisperInputEvent(GetNetEntity(args.Action), GetNetEntity(args.Target)),
            largeInput: true,
            characterLimit: queen.Comp.CharacterLimit,
            minCharacterLimit: 1,
            smartCheck: true);
    }

    private void OnWhisperInput(Entity<XenoPsychicCommunicationComponent> queen, ref XenoPsychicWhisperInputEvent args)
    {
        if (!CanUseQueen(queen))
            return;

        var text = CleanMessage(queen, args.Message);
        if (text == null)
            return;

        if (!TryGetEntity(args.Target, out var target) ||
            !TryGetAction(queen, args.Action, out var action) ||
            !CanWhisperTo(queen, target.Value))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-psychic-target-invalid"), queen, queen, PopupType.MediumCaution);
            return;
        }

        SendPsychicMessage(queen, target.Value, text, PsychicMessageKind.Whisper);
        SendQueenConfirmation(queen, Loc.GetString("rmc-xeno-psychic-whisper-sent", ("target", QueenTargetName(queen, target.Value))));
        var escaped = FormattedMessage.EscapeText(text);
        SendGhostCopy(queen, text, Loc.GetString("rmc-xeno-psychic-ghost-whisper", ("queen", queen.Owner), ("target", target.Value), ("message", escaped)));
        _adminLog.Add(LogType.RMCXenoPsychic, LogImpact.Low, $"Psychic whisper from {ToPrettyString(queen):user} to {ToPrettyString(target.Value):target}: {text}");
        _actions.StartUseDelay(action);
    }

    private void OnRadianceAction(Entity<XenoPsychicCommunicationComponent> queen, ref XenoPsychicRadianceActionEvent args)
    {
        if (args.Handled || !CanUseQueen(queen))
            return;

        if (!_plasma.HasPlasmaPopup(queen.Owner, queen.Comp.RadiancePlasmaCost))
            return;

        _dialog.OpenInput(
            queen,
            Loc.GetString("rmc-xeno-psychic-radiance-message"),
            new XenoPsychicRadianceInputEvent(GetNetEntity(args.Action)),
            largeInput: true,
            characterLimit: queen.Comp.CharacterLimit,
            minCharacterLimit: 1,
            smartCheck: true);
    }

    private void OnRadianceInput(Entity<XenoPsychicCommunicationComponent> queen, ref XenoPsychicRadianceInputEvent args)
    {
        if (!CanUseQueen(queen))
            return;

        var text = CleanMessage(queen, args.Message);
        if (text == null)
            return;

        if (!TryGetAction(queen, args.Action, out var action))
            return;

        var recipients = GetRecipients(queen, queen.Comp.RadianceRange);
        if (recipients.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-psychic-radiance-no-targets"), queen, queen, PopupType.MediumCaution);
            return;
        }

        if (!_plasma.TryRemovePlasmaPopup(queen.Owner, queen.Comp.RadiancePlasmaCost, predicted: false))
            return;

        foreach (var recipient in recipients)
        {
            SendPsychicMessage(queen, recipient, text, PsychicMessageKind.Radiance);
        }

        SendQueenConfirmation(queen, Loc.GetString("rmc-xeno-psychic-radiance-sent", ("count", recipients.Count)));
        var escaped = FormattedMessage.EscapeText(text);
        SendGhostCopy(queen, text, Loc.GetString("rmc-xeno-psychic-ghost-radiance", ("queen", queen.Owner), ("count", recipients.Count), ("message", escaped)));
        LogPsychicRadiance(queen, recipients, text);
        _actions.StartUseDelay(action);
    }

    private void OnGiveOrderAction(Entity<XenoPsychicCommunicationComponent> queen, ref XenoGiveOrderActionEvent args)
    {
        if (args.Handled || !CanUseQueen(queen))
            return;

        if (!_plasma.HasPlasmaPopup(queen.Owner, queen.Comp.GiveOrderPlasmaCost))
            return;

        if (!TryGetWatchedXeno(queen, out var watched))
            return;

        _dialog.OpenInput(
            queen,
            Loc.GetString("rmc-xeno-psychic-give-order-message", ("target", watched)),
            new XenoGiveOrderInputEvent(GetNetEntity(args.Action), GetNetEntity(watched)),
            largeInput: true,
            characterLimit: queen.Comp.CharacterLimit,
            minCharacterLimit: 1,
            smartCheck: true);
    }

    private void OnGiveOrderInput(Entity<XenoPsychicCommunicationComponent> queen, ref XenoGiveOrderInputEvent args)
    {
        if (!CanUseQueen(queen))
            return;

        var text = CleanMessage(queen, args.Message);
        if (text == null)
            return;

        if (!TryGetEntity(args.Target, out var target) ||
            !TryGetAction(queen, args.Action, out var action) ||
            !TryGetWatchedXeno(queen, out var watched) ||
            watched != target.Value)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-psychic-target-invalid"), queen, queen, PopupType.MediumCaution);
            return;
        }

        if (!_plasma.TryRemovePlasmaPopup(queen.Owner, queen.Comp.GiveOrderPlasmaCost, predicted: false))
            return;

        SendPsychicMessage(queen, target.Value, text, PsychicMessageKind.Order);
        SendQueenConfirmation(queen, Loc.GetString("rmc-xeno-psychic-give-order-sent", ("target", target.Value)));
        var escaped = FormattedMessage.EscapeText(text);
        SendGhostCopy(queen, text, Loc.GetString("rmc-xeno-psychic-ghost-order", ("queen", queen.Owner), ("target", target.Value), ("message", escaped)));
        _adminLog.Add(LogType.RMCXenoPsychic, LogImpact.Low, $"Psychic order from {ToPrettyString(queen):user} to {ToPrettyString(target.Value):target}: {text}");
        _actions.StartUseDelay(action);
    }

    private List<EntityUid> GetRecipients(Entity<XenoPsychicCommunicationComponent> queen, float range)
    {
        var recipients = new List<EntityUid>();
        _nearbyMobs.Clear();
        _lookup.GetEntitiesInRange(Transform(queen).Coordinates, range, _nearbyMobs);

        foreach (var recipient in _nearbyMobs)
        {
            if (!IsValidRecipient(queen, recipient, range))
                continue;

            recipients.Add(recipient);
        }

        recipients.Sort((a, b) => string.CompareOrdinal(Name(a), Name(b)));
        return recipients;
    }

    private bool CanUseQueen(Entity<XenoPsychicCommunicationComponent> queen)
    {
        if (!HasComp<ActorComponent>(queen))
            return false;

        if (_mobState.IsDead(queen))
            return false;

        return true;
    }

    private bool CanWhisperTo(Entity<XenoPsychicCommunicationComponent> queen, EntityUid target)
    {
        return IsValidRecipient(queen, target, queen.Comp.WhisperRange);
    }

    private bool IsValidRecipient(Entity<XenoPsychicCommunicationComponent> queen, EntityUid target, float range)
    {
        if (queen.Owner == target || TerminatingOrDeleted(target))
            return false;

        if (!HasComp<ActorComponent>(target) ||
            !HasComp<MobStateComponent>(target) ||
            _mobState.IsDead(target))
        {
            return false;
        }

        return _transform.InRange(queen.Owner, target, range);
    }

    private bool TryGetWatchedXeno(Entity<XenoPsychicCommunicationComponent> queen, out EntityUid watched)
    {
        watched = default;
        if (!_watch.TryGetWatched(queen.Owner, out watched) ||
            watched == queen.Owner)
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-psychic-give-order-must-watch"), queen, queen, PopupType.MediumCaution);
            return false;
        }

        if (!HasComp<XenoComponent>(watched) ||
            !HasComp<ActorComponent>(watched) ||
            _mobState.IsDead(watched) ||
            !_hive.FromSameHive(queen.Owner, watched))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-psychic-target-invalid"), queen, queen, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private bool TryGetAction(Entity<XenoPsychicCommunicationComponent> queen, NetEntity netAction, out EntityUid action)
    {
        action = default;
        if (!TryGetEntity(netAction, out var actionId) ||
            !TryComp(actionId, out ActionComponent? actionComp) ||
            actionComp.AttachedEntity != queen.Owner ||
            !actionComp.Enabled ||
            _actions.IsCooldownActive(actionComp, _timing.CurTime))
        {
            return false;
        }

        action = actionId.Value;
        return true;
    }

    private string? CleanMessage(Entity<XenoPsychicCommunicationComponent> queen, string message)
    {
        message = FormattedMessage.RemoveMarkupPermissive(message).Trim();
        if (string.IsNullOrWhiteSpace(message))
            return null;

        if (message.Length > queen.Comp.CharacterLimit)
            message = message[..queen.Comp.CharacterLimit].Trim();

        message = NewLineRegex.Replace(message, "\n\n");
        return _chat.SanitizeMessageReplaceWords(queen, message);
    }

    private void SendPsychicMessage(Entity<XenoPsychicCommunicationComponent> queen, EntityUid target, string message, PsychicMessageKind kind)
    {
        if (!TryComp(target, out ActorComponent? actor))
            return;

        var escapedMessage = FormattedMessage.EscapeText(message);
        var queenName = FormattedMessage.EscapeText(Name(queen));
        var wrapped = kind switch
        {
            PsychicMessageKind.Order => Loc.GetString("rmc-xeno-psychic-message-order", ("queen", queenName), ("message", escapedMessage)),
            _ when IsSameHiveXeno(queen, target) => Loc.GetString("rmc-xeno-psychic-message-xeno", ("queen", queenName), ("message", escapedMessage)),
            _ => Loc.GetString("rmc-xeno-psychic-message-alien", ("message", escapedMessage)),
        };

        var author = CompOrNull<ActorComponent>(queen)?.PlayerSession.UserId;
        _chat.ChatMessageToOne(
            ChatChannel.Local,
            message,
            wrapped,
            queen,
            false,
            actor.PlayerSession.Channel,
            PsychicColor,
            recordReplay: true,
            author: author);
    }

    private void SendQueenConfirmation(Entity<XenoPsychicCommunicationComponent> queen, string message)
    {
        _popup.PopupEntity(message, queen, queen, PopupType.Medium);

        if (!TryComp(queen, out ActorComponent? actor))
            return;

        _chat.ChatMessageToOne(
            ChatChannel.Local,
            message,
            message,
            default,
            false,
            actor.PlayerSession.Channel,
            PsychicColor,
            recordReplay: true,
            author: actor.PlayerSession.UserId);
    }

    private void SendGhostCopy(Entity<XenoPsychicCommunicationComponent> queen, string message, string wrapped)
    {
        var ghosts = Filter.Empty().AddWhereAttachedEntity(HasComp<GhostComponent>);
        if (ghosts.Count == 0)
            return;

        var author = CompOrNull<ActorComponent>(queen)?.PlayerSession.UserId;
        _chat.ChatMessageToMany(
            message,
            wrapped,
            ghosts,
            ChatChannel.Local,
            queen,
            colorOverride: PsychicColor,
            recordReplay: true,
            author: author);
    }

    private void LogPsychicRadiance(Entity<XenoPsychicCommunicationComponent> queen, List<EntityUid> recipients, string message)
    {
        foreach (var recipient in recipients)
        {
            _adminLog.Add(
                LogType.RMCXenoPsychic,
                LogImpact.Low,
                $"Psychic radiance from {ToPrettyString(queen):user} to {ToPrettyString(recipient):target} ({recipients.Count} recipients total): {message}");
        }
    }

    private bool IsSameHiveXeno(Entity<XenoPsychicCommunicationComponent> queen, EntityUid target)
    {
        return HasComp<XenoComponent>(target) && _hive.FromSameHive(queen.Owner, target);
    }

    private object QueenTargetName(Entity<XenoPsychicCommunicationComponent> queen, EntityUid target)
    {
        if (IsSameHiveXeno(queen, target))
            return target;

        return Loc.GetString("rmc-xeno-psychic-target-unknown");
    }

    private enum PsychicMessageKind
    {
        Whisper,
        Radiance,
        Order,
    }
}
