using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Chat;

public abstract class SharedCMChatSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MarineComponent, ChatGetPrefixEvent>(OnMarineGetPrefix);
        SubscribeLocalEvent<XenoComponent, ChatGetPrefixEvent>(OnXenoGetPrefix);
    }

    private void OnMarineGetPrefix(Entity<MarineComponent> ent, ref ChatGetPrefixEvent args)
    {
        if (args.Channel?.ID == SharedChatSystem.HivemindChannel)
            args.Channel = null;
    }

    private void OnXenoGetPrefix(Entity<XenoComponent> ent, ref ChatGetPrefixEvent args)
    {
        if (args.Channel?.ID != SharedChatSystem.HivemindChannel)
            args.Channel = null;
    }

    public virtual string SanitizeMessageReplaceWords(EntityUid source, string msg)
    {
        return msg;
    }

    public virtual void ChatMessageToOne(
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
    }

    public virtual void ChatMessageToOne(
        string message,
        EntityUid target,
        ChatChannel channel = ChatChannel.Local,
        bool hideChat = false,
        Color? colorOverride = null,
        bool recordReplay = false,
        string? audioPath = null,
        float audioVolume = 0,
        NetUserId? author = null)
    {
        if (!TryComp(target, out ActorComponent? actor))
            return;

        ChatMessageToOne(channel,
            message,
            message,
            default,
            hideChat,
            actor.PlayerSession.Channel,
            colorOverride,
            recordReplay,
            audioPath,
            audioVolume,
            author
        );
    }
}
