using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared._CM14.Chat;
using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Xenos;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Chat.Chat;

public sealed class CMChatSystem : SharedCMChatSystem
{
    [Dependency] private readonly ReplacementAccentSystem _wordreplacement = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> ChatSanitize = "CMChatSanitize";
    private static readonly ProtoId<ReplacementAccentPrototype> MarineChatSanitize = "CMChatSanitizeMarine";
    private static readonly ProtoId<ReplacementAccentPrototype> XenoChatSanitize = "CMChatSanitizeXeno";

    private readonly List<ICommonSession> _toRemove = new();

    public override void Initialize()
    {
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

    public string SanitizeMessageReplaceWords(EntityUid source, string msg)
    {
        msg = _wordreplacement.ApplyReplacements(msg, ChatSanitize);

        var factionSanitize = HasComp<XenoComponent>(source) ? XenoChatSanitize : MarineChatSanitize;
        msg = _wordreplacement.ApplyReplacements(msg, factionSanitize);

        return msg;
    }
}
