using Robust.Client.Graphics;

namespace Content.Client._RMC14.Weapons.Ranged.SmartGun;

public sealed class GunAimAssistOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        _overlay.AddOverlay(new GunAimAssistOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<GunAimAssistOverlay>();
        base.Shutdown();
    }
}
