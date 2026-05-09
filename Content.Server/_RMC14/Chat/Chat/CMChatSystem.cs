using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Chat.Managers;
using Content.Server.Radio.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Speech.Prototypes;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Marines;
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
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ReplacementAccentSystem _wordreplacement = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> ChatSanitize = "CMChatSanitize";
    private static readonly ProtoId<ReplacementAccentPrototype> MarineChatSanitize = "CMChatSanitizeMarine";
    private static readonly ProtoId<ReplacementAccentPrototype> XenoChatSanitize = "CMChatSanitizeXeno";

    private readonly List<ICommonSession> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
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

    private bool IsValidRadioPrefix(EntityUid source, string prefixPart)
    {
        if (prefixPart.Length != 2)
            return false;

        var radioQuery = GetEntityQuery<ActiveRadioComponent>();
        if (!radioQuery.HasComponent(source))
            return false;

        var hasRadio = radioQuery.GetComponent(source);
        var allChannels = _proto.EnumeratePrototypes<RadioChannelPrototype>();

        var prefix = prefixPart[0];
        var keycode = char.ToLowerInvariant(prefixPart[1]);

        foreach (var ch in allChannels)
        {
            if (!hasRadio.Channels.Contains(ch.ID))
                continue;

            if (ch.RadioPrefix == prefix && ch.KeyCode == keycode)
            {
                return true;
            }
        }

        return false;
    }

    public List<string>? TryMultiBroadcast(EntityUid source, string message)
    {
        if (string.IsNullOrEmpty(message) || message.Length < 2)
            return null;

        if (!HasComp<InventoryComponent>(source))
            return null;

        var radioQuery = GetEntityQuery<ActiveRadioComponent>();
        if (!radioQuery.HasComponent(source))
            return null;

        var hasRadio = radioQuery.GetComponent(source);
        var allChannels = _proto.EnumeratePrototypes<RadioChannelPrototype>();

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
        var validPrefixes = new List<string>();

        var i = 0;
        while (i + 1 < message.Length)
        {
            var prefix = message[i];
            var keycode = char.ToLowerInvariant(message[i + 1]);
            var found = false;

            foreach (var ch in allChannels)
            {
                if (!hasRadio.Channels.Contains(ch.ID))
                    continue;

                if (ch.RadioPrefix == prefix && ch.KeyCode == keycode)
                {
                    validPrefixes.Add(message.Substring(i, 2));
                    i += 2;
                    found = true;
                    break;
                }
            }

            if (!found)
                i++;
        }

        if (validPrefixes.Count < 2)
            return null;

        var count = Math.Min(validPrefixes.Count, headset.Value.Comp.Maximum);
        validPrefixes = validPrefixes.Take(count).ToList();

        for (var idx = 0; idx < validPrefixes.Count; idx++)
        {
            var subMsg = message;
            for (var j = 0; j < validPrefixes.Count; j++)
            {
                if (idx == j)
                    continue;

                var toRemove = validPrefixes[j];
                var index = subMsg.IndexOf(toRemove, StringComparison.Ordinal);
                if (index >= 0)
                {
                    subMsg = subMsg.Remove(index, toRemove.Length);
                }
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
