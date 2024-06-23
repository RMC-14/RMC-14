using Content.Shared._RMC14.Marines;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Marines;

public sealed class MarineSystem : SharedMarineSystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlays.AddOverlay(new MarineOverlay());
    }
}
