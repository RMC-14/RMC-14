using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Weapons.Ranged.Sniper.Focused;

public sealed partial class RMCFocusingSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new FocusedOverlay(EntityManager, _player, _timing));
    }
}
