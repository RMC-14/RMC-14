using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids.Burrow;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Xenonids.Burrow;

public sealed partial class XenoBurrowSystem : SharedXenoBurrowSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var localEntity = _player.LocalEntity;

        var burrowQuery = EntityQueryEnumerator<XenoBurrowComponent, SpriteComponent, RMCNightVisionVisibleComponent>();

        while (burrowQuery.MoveNext(out var xeno, out var burrowed, out _, out var nightVision))
        {
            if (localEntity != xeno)
                _sprite.SetVisible(xeno, !burrowed.Active);
            else
                _sprite.SetColor(xeno, burrowed.Active ? Color.White.WithAlpha(0.4f) : Color.White);

            if (burrowed.Active)
                nightVision.Transparency = 0.4f;
            else
                nightVision.Transparency = null;
        }
    }
}
