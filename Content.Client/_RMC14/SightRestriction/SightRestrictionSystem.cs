using Content.Shared._RMC14.SightRestriction;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.SightRestriction;

public sealed class SightRestrictionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private SightRestrictionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SightRestrictionComponent, ComponentInit>(OnEyeProtectionInit);
        SubscribeLocalEvent<SightRestrictionComponent, ComponentShutdown>(OnEyeProtectionShutdown);

        SubscribeLocalEvent<SightRestrictionComponent, LocalPlayerAttachedEvent>(OnEyeProtectionAttached);
        SubscribeLocalEvent<SightRestrictionComponent, LocalPlayerDetachedEvent>(OnEyeProtectionDetached);

        _overlay = new();
    }

    private void OnEyeProtectionAttached(EntityUid uid, SightRestrictionComponent component,  LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnEyeProtectionDetached(EntityUid uid, SightRestrictionComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnEyeProtectionInit(EntityUid uid, SightRestrictionComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnEyeProtectionShutdown(EntityUid uid, SightRestrictionComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayManager.RemoveOverlay(_overlay);
        }
    }
}
