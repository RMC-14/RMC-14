using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Word;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Announce;

public sealed class XenoAnnounceSystem : SharedXenoAnnounceSystem
{
    private static readonly ProtoId<AnnouncementPresetPrototype> QueenAnnouncementPreset = "XenoQueen";

    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly AnnouncementRouterSystem _announcementRouter = default!;

    public override void Announce(EntityUid source, Filter filter, string message, string wrapped, SoundSpecifier? sound = null, PopupType? popup = null, bool needsQueen = false)
    {
        base.Announce(source, filter, message, wrapped, sound, popup, needsQueen);

        if (needsQueen)
        {
            if (Hive.GetHive(source) is { } sourceHive)
            {
                if (!Hive.HasHiveQueen(sourceHive))
                    return;
            }
            else
            {
                return;
            }
        }

        filter.AddWhereAttachedEntity(HasComp<GhostComponent>);

        if (source.IsValid())
            _adminLogs.Add(LogType.RMCXenoAnnounce, $"{ToPrettyString(source):source} xeno announced message: {message}");

        var channels = AnnouncementChannels.Chat | AnnouncementChannels.Sound;
        if (source.IsValid() && IsQueenAnnouncementSource(source))
            channels |= AnnouncementChannels.Overlay;

        if (popup != null)
            channels |= AnnouncementChannels.Popup;

        _announcementRouter.Announce(new AnnouncementRequest
        {
            Message = message,
            Preset = QueenAnnouncementPreset,
            Route = new AnnouncementRoute
            {
                Target = AnnouncementTarget.Xenos,
                Speaker = source.IsValid() ? source : null,
                Source = source.IsValid() ? source : null,
                Channels = channels,
            },
            Chat = new AnnouncementChatOptions
            {
                Message = message,
                WrappedMessage = wrapped,
                Channel = ChatChannel.Radio,
            },
            Sound = new AnnouncementSoundOptions
            {
                Sound = sound,
            },
            Popup = popup == null
                ? null
                : new AnnouncementPopupOptions
                {
                    Type = popup.Value,
                    Message = message,
                }
        }, filter);
    }

    private bool IsQueenAnnouncementSource(EntityUid source)
    {
        return HasComp<XenoWordQueenComponent>(source) ||
               HasComp<XenoEvolutionGranterComponent>(source);
    }
}
