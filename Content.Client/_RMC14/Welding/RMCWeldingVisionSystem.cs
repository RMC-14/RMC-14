using Robust.Client.Graphics;

namespace Content.Client._RMC14.Welding;

public sealed class RMCWeldingVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RMCWeldingVisionOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<RMCWeldingVisionOverlay>();
    }
}
