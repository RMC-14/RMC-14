using Content.Shared._RMC14.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteSystem : SharedRMCSpriteSystem
{
    public override void Update(float frameTime)
    {
        var colors = EntityQueryEnumerator<SpriteColorComponent, SpriteComponent>();
        while (colors.MoveNext(out var color, out var sprite))
        {
            sprite.Color = color.Color;
        }
    }
}
