using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.HiveLeader;

public sealed class HiveLeaderUISystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        if (!_overlay.HasOverlay<HiveLeaderOverlay>())
            _overlay.AddOverlay(new HiveLeaderOverlay());
    }

    public override void Shutdown()
    {
        _overlay.RemoveOverlay<HiveLeaderOverlay>();
    }
}
