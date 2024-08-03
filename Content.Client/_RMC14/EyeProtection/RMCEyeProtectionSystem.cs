using Content.Shared._RMC14.EyeProtection;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.EyeProtection;

public sealed class RMCEyeProtectionSystem : RMCSharedEyeProtectionSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private RMCEyeProtectionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCEyeProtectionComponent, ComponentInit>(OnEyeProtectionInit);
        SubscribeLocalEvent<RMCEyeProtectionComponent, ComponentShutdown>(OnEyeProtectionShutdown);

        SubscribeLocalEvent<RMCEyeProtectionComponent, LocalPlayerAttachedEvent>(OnEyeProtectionAttached);
        SubscribeLocalEvent<RMCEyeProtectionComponent, LocalPlayerDetachedEvent>(OnEyeProtectionDetached);

        _overlay = new();
    }

    private void OnEyeProtectionAttached(EntityUid uid, RMCEyeProtectionComponent component,  LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnEyeProtectionDetached(EntityUid uid, RMCEyeProtectionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnEyeProtectionInit(EntityUid uid, RMCEyeProtectionComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnEyeProtectionShutdown(EntityUid uid, RMCEyeProtectionComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayManager.RemoveOverlay(_overlay);
        }
    }
}
