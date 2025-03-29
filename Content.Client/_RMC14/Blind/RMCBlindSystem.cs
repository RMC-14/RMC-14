using Robust.Client.Graphics;

namespace Content.Client._RMC14.Blind;

public sealed class RMCBlindSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RMCBlurOverlay(EntityManager));
    }
}
