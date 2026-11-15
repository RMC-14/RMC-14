using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.Targeting;

public sealed class XenoAbilityPreviewSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new XenoAbilityPreviewOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<XenoAbilityPreviewOverlay>();
    }
}
