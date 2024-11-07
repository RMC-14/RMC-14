using Content.Shared._RMC14.Teleporter;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Teleporter;

public sealed class RMCTeleporterSystem : SharedRMCTeleporterSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RMCTeleporterViewerOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<RMCTeleporterViewerOverlay>();
    }
}
