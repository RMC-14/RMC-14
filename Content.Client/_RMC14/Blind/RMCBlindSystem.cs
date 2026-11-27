using Robust.Client.Graphics;

namespace Content.Client._RMC14.Blind;

public sealed partial class RMCBlindSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RMCBlurOverlay(EntityManager));
    }
}
