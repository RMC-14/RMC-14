using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Chat.Managers;
using Content.Server.Speech.EntitySystems;
using Content.Server.Speech.Prototypes;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Chat.Chat;

public sealed class CMChatSystem : SharedCMChatSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ReplacementAccentSystem _wordreplacement = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> ChatSanitize = "CMChatSanitize";
    private static readonly ProtoId<ReplacementAccentPrototype> MarineChatSanitize = "CMChatSanitizeMarine";
    private static readonly ProtoId<ReplacementAccentPrototype> XenoChatSanitize = "CMChatSanitizeXeno";
    private static readonly Regex PrefixesRegex = new(@"^:(\w)+");

    private readonly List<ICommonSession> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineComponent, ChatMessageAfterGetRecipients>(OnMarineAfterGetRecipients);
        SubscribeLocalEvent<XenoComponent, ChatMessageAfterGetRecipients>(OnXenoAfterGetRecipients);
    }

    private void OnMarineAfterGetRecipients(Entity<MarineComponent> ent, ref ChatMessageAfterGetRecipients args)
    {
        _toRemove.Clear();

        foreach (var (session, data) in args.Recipients)
        {
            if (data.Observer)
                continue;

            if (HasComp<XenoComponent>(session.AttachedEntity))
                _toRemove.Add(session);
        }

        foreach (var session in _toRemove)
        {
            args.Recipients.Remove(session);
        }
    }

    private void OnXenoAfterGetRecipients(Entity<XenoComponent> ent, ref ChatMessageAfterGetRecipients args)
    {
        _toRemove.Clear();

        foreach (var (session, data) in args.Recipients)
        {
            if (data.Observer)
                continue;

            if (!HasComp<XenoComponent>(session.AttachedEntity))
                _toRemove.Add(session);
        }

        foreach (var session in _toRemove)
        {
            args.Recipients.Remove(session);
        }
    }

    public override string SanitizeMessageReplaceWords(EntityUid source, string msg)
    {
        msg = _wordreplacement.ApplyReplacements(msg, ChatSanitize);

        var factionSanitize = HasComp<XenoComponent>(source) ? XenoChatSanitize : MarineChatSanitize;
        msg = _wordreplacement.ApplyReplacements(msg, factionSanitize);

        return msg;
    }

    public override void ChatMessageToOne(
        ChatChannel channel,
        string message,
        string wrappedMessage,
        EntityUid source,
        bool hideChat,
        INetChannel client,
        Color? colorOverride = null,
        bool recordReplay = false,
        string? audioPath = null,
        float audioVolume = 0,
        NetUserId? author = null)
    {
        _chat.ChatMessageToOne(
            channel,
            message,
            wrappedMessage,
            source,
            hideChat,
            client,
            colorOverride,
            recordReplay,
            audioPath,
            audioVolume,
            author
        );
    }

    public override void ChatMessageToMany(
        string message,
        string wrappedMessage,
        Filter filter,
        ChatChannel channel,
        EntityUid source = default,
        bool hideChat = false,
        Color? colorOverride = null,
        bool recordReplay = false,
        string? audioPath = null,
        float audioVolume = 0,
        NetUserId? author = null)
    {
        if (channel == ChatChannel.Radio && source != default)
        {
            ChatMessageToManyRadio(message, wrappedMessage, filter, source, hideChat, colorOverride, recordReplay, audioPath, audioVolume, author);
            return;
        }

        _chat.ChatMessageToManyFiltered(
            filter,
            channel,
            message,
            wrappedMessage,
            source,
            hideChat,
            recordReplay,
            colorOverride,
            audioPath,
            audioVolume
        );
    }

    // cursed code
    private void ChatMessageToManyRadio(
        string message,
        string wrappedMessage,
        Filter filter,
        EntityUid source,
        bool hideChat = false,
        Color? colorOverride = null,
        bool recordReplay = false,
        string? audioPath = null,
        float audioVolume = 0,
        NetUserId? author = null)
    {
        var hivemindMessage = $";{message}";

        if (!_chatSystem.TryProccessRadioMessage(source, hivemindMessage, out var processedMessage, out var channel))
            return;

        var transformSpeakerEv = new TransformSpeakerNameEvent(source, Name(source));
        RaiseLocalEvent(source, transformSpeakerEv);

        var speechVerb = _chatSystem.GetSpeechVerb(source, processedMessage);
        var sanitizedMessage = SanitizeMessageReplaceWords(source, processedMessage);

        var finalColor = colorOverride ?? channel?.Color ?? Color.White;
        var finalWrappedMessage = FormatRadioMessage(transformSpeakerEv.VoiceName, sanitizedMessage, speechVerb, channel);

        _chat.ChatMessageToManyFiltered(
            filter,
            ChatChannel.Radio,
            sanitizedMessage,
            finalWrappedMessage,
            source,
            hideChat,
            recordReplay,
            finalColor,
            audioPath,
            audioVolume
        );
    }

    private string FormatRadioMessage(string speakerName, string message, SpeechVerbPrototype speechVerb, RadioChannelPrototype? channel)
    {
        var channelName = channel?.Name ?? "Unknown";
        var verb = speechVerb.SpeechVerbStrings.FirstOrDefault() ?? "says";

        var channelColor = channel?.Color.ToHex() ?? "#FFFFFF";

        var formattedMessage = $"[color={channelColor}][bold]\\[{Loc.GetString(channelName)}\\][/bold][/color] [bold]{speakerName}[/bold] {verb}, \"{message}\"";

        if (speechVerb.Bold)
            formattedMessage = $"[bold]{formattedMessage}[/bold]";

        if (speechVerb.FontSize != 12)
            formattedMessage = $"[font size={speechVerb.FontSize}]{formattedMessage}[/font]";

        return formattedMessage;
    }

    public List<string>? TryMultiBroadcast(EntityUid source, string message)
    {
        if (!message.StartsWith(SharedChatSystem.RadioChannelPrefix))
            return null;

        if (message.Length < 3)
            return null;

        if (!_chatSystem._keyCodes.ContainsKey(char.ToLowerInvariant(message[1])) ||
            !_chatSystem._keyCodes.ContainsKey(char.ToLowerInvariant(message[2])))
        {
            return null;
        }

        if (!HasComp<InventoryComponent>(source))
            return null;

        var matches = PrefixesRegex.Matches(message);
        if (matches.Count == 0)
            return null;

        var time = _timing.CurTime;
        Entity<HeadsetMultiBroadcastComponent>? headset = null;
        var ears = _inventory.GetSlotEnumerator(source, SlotFlags.EARS);
        while (ears.MoveNext(out var ear))
        {
            if (ear.ContainedEntity is not { } contained)
                continue;

            if (TryComp(contained, out HeadsetMultiBroadcastComponent? headsetComp))
            {
                headset = (contained, headsetComp);
                break;
            }
        }

        if (headset == null)
            return null;

        var messages = new List<string>();
        var replace = new List<string>();
        var captures = matches[0].Groups[1].Captures;
        var count = Math.Min(captures.Count, headset.Value.Comp.Maximum);
        for (var i = 0; i < count; i++)
        {
            replace.Add(captures[i].Value);
        }

        for (var i = 0; i < replace.Count; i++)
        {
            var subMsg = message;
            for (var j = 0; j < replace.Count; j++)
            {
                if (i == j)
                    continue;

                subMsg = subMsg.Remove(subMsg.IndexOf(replace[j], StringComparison.Ordinal), 1);
            }

            messages.Add(subMsg);
        }

        if (messages.Count < 2)
            return null;

        var timeLeft = headset.Value.Comp.Last + headset.Value.Comp.Cooldown - time;
        if (headset.Value.Comp.Last != null &&
            timeLeft != null &&
            timeLeft.Value > TimeSpan.Zero)
        {
            _popup.PopupEntity(
                $"You've used the multi-broadcast system too recently, wait {timeLeft.Value.TotalSeconds:F0} more seconds.",
                source,
                source,
                PopupType.MediumCaution
            );

            messages.Clear();
            return messages;
        }

        headset.Value.Comp.Last = time;
        Dirty(headset.Value);
        return messages;
    }
}
