using Robust.Client.Graphics;

namespace Content.Client._RMC14.Intel;

public sealed class IntelDetectorOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        if (!_overlay.HasOverlay<IntelDetectorOverlay>())
            _overlay.AddOverlay(new IntelDetectorOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<IntelDetectorOverlay>();
    }
}
