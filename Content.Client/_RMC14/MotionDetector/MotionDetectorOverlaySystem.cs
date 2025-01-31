using Robust.Client.Graphics;

namespace Content.Client._RMC14.MotionDetector;

public sealed class MotionDetectorOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        if (!_overlay.HasOverlay<MotionDetectorOverlay>())
            _overlay.AddOverlay(new MotionDetectorOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<MotionDetectorOverlay>();
    }
}
