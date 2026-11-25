using Robust.Client.Graphics;

namespace Content.Client._RMC14.Welding;

public sealed partial class WeldingVisionSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new WeldingVisionOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<WeldingVisionOverlay>();
    }
}
