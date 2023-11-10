using Content.Shared.CM14.Marines;
using Robust.Client.Graphics;

namespace Content.Client.CM14.Marines;

public sealed class MarineSystem : SharedMarineSystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlays.AddOverlay(new MarineOverlay());
    }
}
