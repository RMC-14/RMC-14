using System.Linq;
using System.Text.RegularExpressions;
using Content.Server._RMC14.Language.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Speech.EntitySystems;
using Content.Server.Speech.Prototypes;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Language.Components;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat;
using Content.Shared.Ghost;
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
    [Dependency] private readonly LanguageSystem _language = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<ImaginaryFriendComponent> _friendComponent;
    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    private static readonly ProtoId<ReplacementAccentPrototype> ChatSanitize = "CMChatSanitize";
    private static readonly ProtoId<ReplacementAccentPrototype> MarineChatSanitize = "CMChatSanitizeMarine";
    private static readonly ProtoId<ReplacementAccentPrototype> XenoChatSanitize = "CMChatSanitizeXeno";
    private static readonly Regex PrefixesRegex = new(@"^:(\w)+");

    private readonly List<ICommonSession> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _friendComponent = GetEntityQuery<ImaginaryFriendComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<LanguageComponent, ChatMessageAfterGetRecipients>(OnLanguageGetRecipients);
        SubscribeLocalEvent<ImaginaryFriendComponent, ChatMessageAfterGetRecipients>(OnImaginaryFriendGetRecipients);
    }

    private void OnLanguageGetRecipients(Entity<LanguageComponent> ent, ref ChatMessageAfterGetRecipients args)
    {
        _toRemove.Clear();

        if (_friendComponent.HasComp(ent))
            return; // handled separately

        var entIsMarine = _marineQuery.HasComp(ent);
        var entIsXeno = _xenoQuery.HasComp(ent);
        foreach (var (session, _) in args.Recipients)
        {
            if (session.AttachedEntity is not { } sessionEntity)
                continue;

            if (_ghostQuery.HasComp(sessionEntity))
                continue;

            // If the message has a language, check if it should be visible to `sessionEntity`.
            if (args.Language is { } spokenLanguage)
            {
                if (!_language.CanSeeSpokenMessage(sessionEntity, spokenLanguage))
                    _toRemove.Add(session);
                continue;
            }

            // If there isn't a language (LOOC for example), just go with the standard "same faction" check.
            if (entIsMarine != _marineQuery.HasComp(sessionEntity) || entIsXeno != _xenoQuery.HasComp(sessionEntity))
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

        foreach (var (session, _) in args.Recipients)
        {
            if (_ghostQuery.HasComp(session.AttachedEntity))
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

        if (keycode == SharedChatSystem.DefaultChannelKey && keys.DefaultChannel != null)
            return true;

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
