using Content.Shared._RMC14.Vents;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.VentCrawl;
public sealed class VentCrawlingSystem : SharedVentCrawlingSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (!_overlay.HasOverlay<VentCrawlIconOverlay>())
            _overlay.AddOverlay(new VentCrawlIconOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<VentCrawlIconOverlay>();
    }
}
