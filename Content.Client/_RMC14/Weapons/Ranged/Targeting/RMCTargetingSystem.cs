using Robust.Client.Graphics;

namespace Content.Client._RMC14.Weapons.Ranged.Targeting;

public sealed class RMCTargetingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new TargetingOverlay(EntityManager));
    }
}
