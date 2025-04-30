using Content.Shared._RMC14.Sprite;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteSystem : SharedRMCSpriteSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Update(float frameTime)
    {
        var colors = EntityQueryEnumerator<SpriteColorComponent, SpriteComponent>();
        while (colors.MoveNext(out var color, out var sprite))
        {
            sprite.Color = color.Color;
        }

        if (_player.LocalEntity is not { } player)
            return;

        if (HasComp<GhostComponent>(player))
            return;

        if (TryComp(player, out SpriteComponent? playerSprite))
            playerSprite.DrawDepth = (int) Shared.DrawDepth.DrawDepth.BelowMobs;
    }
}
