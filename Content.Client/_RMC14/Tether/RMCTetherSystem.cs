using Content.Shared._RMC14.Tether;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Tether;

public sealed partial class RMCTetherSystem : SharedRMCTetherSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RMCTetherOverlay(EntityManager, _playerManager, _timing));
    }
}
