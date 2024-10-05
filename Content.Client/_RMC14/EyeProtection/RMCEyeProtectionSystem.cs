using Content.Shared._RMC14.EyeProtection;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.EyeProtection;

public sealed class RMCEyeProtectionSystem : SharedRMCEyeProtectionSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private RMCEyeProtectionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSightRestrictionComponent, ComponentInit>(OnEyeProtectionInit);
        SubscribeLocalEvent<RMCSightRestrictionComponent, ComponentShutdown>(OnEyeProtectionShutdown);

        SubscribeLocalEvent<RMCSightRestrictionComponent, LocalPlayerAttachedEvent>(OnEyeProtectionAttached);
        SubscribeLocalEvent<RMCSightRestrictionComponent, LocalPlayerDetachedEvent>(OnEyeProtectionDetached);

        _overlay = new();
    }

    private void OnEyeProtectionAttached(EntityUid uid, RMCSightRestrictionComponent component,  LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnEyeProtectionDetached(EntityUid uid, RMCSightRestrictionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnEyeProtectionInit(EntityUid uid, RMCSightRestrictionComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnEyeProtectionShutdown(EntityUid uid, RMCSightRestrictionComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayManager.RemoveOverlay(_overlay);
        }
    }
}
