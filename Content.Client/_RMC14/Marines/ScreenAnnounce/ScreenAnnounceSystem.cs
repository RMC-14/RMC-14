using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.ScreenAnnounce;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Marines.ScreenAnnounce;

public sealed class ScreenAnnounceSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private ScreenAnnounceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ScreenAnnounceMessage>(OnAnnounce);
        SubscribeLocalEvent<MarineComponent, ComponentShutdown>(OnShutdown);
        
        _overlay = new ScreenAnnounceOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnAnnounce(ScreenAnnounceMessage args)
    {
        if (_uiManager.GetUIController<ScreenAnnounceUIController>() is { } uiController)
        {
            uiController.UpdateAnnouncement(args.AnnounceText, args.Target, args.ScreenAnnounceArgs, args.StartingMessage, args.Squad);
        }
    }

    private void OnShutdown(EntityUid uid, MarineComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.RemoveOverlay(_overlay);
    }
}