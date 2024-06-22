using Content.Server.Chat.Managers;
using Content.Shared._CM14.Xenonids.Announce;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._CM14.Announce;

public sealed class XenoAnnounceSystem : SharedXenoAnnounceSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Announce(EntityUid source, Filter filter, string message, string wrapped, SoundSpecifier? sound = null, PopupType? popup = null)
    {
        base.Announce(source, filter, message, wrapped, sound, popup);

        _chat.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrapped, source, false, true, null);
        _audio.PlayGlobal(sound, filter, true);

        if (popup != null)
        {
            foreach (var session in filter.Recipients)
            {
                if (session.AttachedEntity is { } recipient)
                    _popup.PopupEntity(message, recipient, recipient, popup.Value);
            }
        }
    }
}
