using Content.Server._RMC14.Announce.Core;
using Content.Server._RMC14.Announce.Validation;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.Announce;
using Content.Shared.Chat;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Announce;

public sealed partial class AnnouncementRouterSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly AnnouncementOverlaySystem _overlay = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private AnnouncementValidator _validator = default!;
    private AnnouncementPresetResolver _presetResolver = default!;
    private AnnouncementTargetFilter _targetFilter = default!;

    public override void Initialize()
    {
        _validator = new AnnouncementValidator();
        _presetResolver = new AnnouncementPresetResolver(_prototypes);
        _targetFilter = new AnnouncementTargetFilter(EntityManager);
    }

    public void Announce(AnnouncementRequest request)
    {
        var filter = _targetFilter.Build(request.Route.Target);
        Announce(request, filter);
    }

    public void Announce(AnnouncementRequest request, Filter filter)
    {
        if (!ValidateRequest(request) || filter.Count == 0)
            return;

        var preset = _presetResolver.Resolve(request.Preset);

        if (request.Route.Channels.HasFlag(AnnouncementChannels.Overlay) && preset != null)
            _overlay.Dispatch(request, preset, filter);

        if (request.Route.Channels.HasFlag(AnnouncementChannels.Chat))
            DispatchChat(request, filter);

        if (request.Route.Channels.HasFlag(AnnouncementChannels.Sound))
            DispatchSound(request, preset, filter);

        if (request.Route.Channels.HasFlag(AnnouncementChannels.Popup))
            DispatchPopup(request, filter);
    }

    private bool ValidateRequest(AnnouncementRequest request)
    {
        var validation = _validator.ValidateRequest(request);
        if (validation.IsValid)
            return true;

        Log.Warning($"Invalid announcement request: {validation.GetErrorSummary()}");
        return false;
    }

    private void DispatchChat(AnnouncementRequest request, Filter filter)
    {
        var chatMessage = request.Chat?.Message ?? request.Message;
        var wrapped = request.Chat?.WrappedMessage ?? chatMessage;

        _chat.ChatMessageToManyFiltered(
            filter,
            request.Chat?.Channel ?? ChatChannel.Radio,
            chatMessage,
            wrapped,
            request.Route.Source ?? default,
            false,
            true,
            null);
    }

    private void DispatchSound(AnnouncementRequest request, AnnouncementPresetPrototype? preset, Filter filter)
    {
        var sound = request.Sound?.Sound ?? preset?.Sound;
        if (sound == null)
            return;

        AudioParams? audioParams = null;
        if (request.Sound?.Volume is { } volume)
            audioParams = AudioParams.Default.WithVolume(volume);

        _audio.PlayGlobal(sound, filter, true, audioParams);
    }

    private void DispatchPopup(AnnouncementRequest request, Filter filter)
    {
        if (request.Popup is not { } popup)
            return;

        var message = popup.Message ?? request.Message;
        foreach (var session in filter.Recipients)
        {
            if (session.AttachedEntity is { } recipient)
                _popup.PopupEntity(message, recipient, recipient, popup.Type);
        }
    }
}
