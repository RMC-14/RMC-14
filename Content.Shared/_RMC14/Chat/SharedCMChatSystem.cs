using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Chat;

public abstract class SharedCMChatSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SquadSystem _squadSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MarineComponent, ChatGetPrefixEvent>(OnMarineGetPrefix);
        SubscribeLocalEvent<XenoComponent, ChatGetPrefixEvent>(OnXenoGetPrefix);
    }

    private void OnMarineGetPrefix(Entity<MarineComponent> ent, ref ChatGetPrefixEvent args)
    {
        if (args.Channel != null && args.Channel.IsXenoHivemind)
            args.Channel = null;
    }

    private void OnXenoGetPrefix(Entity<XenoComponent> ent, ref ChatGetPrefixEvent args)
    {
        if (args.Channel != null && !args.Channel.IsXenoHivemind)
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

    public void ChatMessageToOne(
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

    public virtual void ChatMessageToMany(
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
    }

    public virtual void Emote(
        EntityUid source,
        string message,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false)
    {
    }

    public string? ColorizeSpeakerNameBySquadOrNull(ChatMessage msg)
    {
        var colorMode = _config.GetCVar(RMCCVars.RMCChatSquadColorMode);
        Color? squadColor = null;

        if (colorMode == true && _squadSystem.TryGetSquadMemberColor(GetEntity(msg.SenderEntity), out var color, accessible: true))
            squadColor = color;

        if (squadColor != null)
        {
            msg.WrappedMessage = SharedChatSystem.InjectTagInsideTag(
                msg,
                outerTag: "Name",
                innerTag: "color",
                tagParameter: squadColor.Value.ToHex());
            return msg.WrappedMessage;
        }

        return null;
    }
}
