using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Chat.Managers;
using Content.Server.Radio.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Speech.Prototypes;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Chat.Chat;

public sealed class CMChatSystem : SharedCMChatSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ReplacementAccentSystem _wordreplacement = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

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
        SubscribeLocalEvent<ImaginaryFriendComponent, ChatMessageAfterGetRecipients>(OnImaginaryFriendGetRecipients);
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

    private void OnImaginaryFriendGetRecipients(Entity<ImaginaryFriendComponent> ent, ref ChatMessageAfterGetRecipients args)
    {
        _toRemove.Clear();

        foreach (var (session, data) in args.Recipients)
        {
            if (data.Observer)
                continue;

            if (ent.Comp.Imaginer != session.AttachedEntity)
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

    private bool IsValidRadioPrefix(EntityUid headset, string prefixPart)
    {
        if (prefixPart.Length != 2)
            return false;

        if (!TryComp(headset, out EncryptionKeyHolderComponent? keys))
            return false;

        var prefix = prefixPart[0];
        if (prefix == SharedChatSystem.RadioChannelAltPrefix)
            prefix = SharedChatSystem.RadioChannelPrefix;

        var keycode = char.ToLowerInvariant(prefixPart[1]);

        foreach (var ch in _proto.EnumeratePrototypes<RadioChannelPrototype>())
        {
            if (!keys.Channels.Contains(ch.ID))
                continue;

            if (ch.RadioPrefix == prefix && ch.KeyCode == keycode)
                return true;
        }

        return false;
    }

    private bool IsValidRadioKey(EntityUid headset, char prefix, char keycode)
    {
        return IsValidRadioPrefix(headset, $"{prefix}{char.ToLowerInvariant(keycode)}");
    }

    public List<string>? TryMultiBroadcast(EntityUid source, string message)
    {
        if (string.IsNullOrEmpty(message) || message.Length < 2)
            return null;

        if (!HasComp<InventoryComponent>(source))
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

        var validPrefixes = new List<string>();
        var prefixLength = 0;
        var sharedPrefix = message[0];

        if (sharedPrefix != SharedChatSystem.RadioChannelPrefix &&
            sharedPrefix != SharedChatSystem.RadioChannelAltPrefix)
            return null;

        for (var i = 1; i < message.Length; i++)
        {
            var keycode = char.ToLowerInvariant(message[i]);
            if (char.IsWhiteSpace(keycode))
            {
                prefixLength = i;
                break;
            }

            if (!IsValidRadioKey(headset.Value, sharedPrefix, keycode))
            {
                prefixLength = i;
                break;
            }

            validPrefixes.Add($"{sharedPrefix}{keycode}");
            prefixLength = i + 1;
        }

        var count = Math.Min(validPrefixes.Count, headset.Value.Comp.Maximum);
        validPrefixes = validPrefixes.Take(count).ToList();

        if (validPrefixes.Count < 2)
            return null;

        var messages = new List<string>(validPrefixes.Count);
        var messageBody = message[prefixLength..];

        for (var idx = 0; idx < validPrefixes.Count; idx++)
            messages.Add($"{validPrefixes[idx]}{messageBody}");

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
