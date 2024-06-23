using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.Hud;

public sealed class XenoHudSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        if (!_overlay.HasOverlay<XenoHudOverlay>())
            _overlay.AddOverlay(new XenoHudOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<XenoHudOverlay>();
    }
}
