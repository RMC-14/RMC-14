using Content.Shared._RMC14.Stealth;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Stealth;

public sealed class EntityInvisibilityVisualsSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var invisible = EntityQueryEnumerator<EntityTurnInvisibleComponent, SpriteComponent>();
        while (invisible.MoveNext(out var uid, out var comp, out var sprite))
        {
            var opacity =  TryComp<EntityActiveInvisibleComponent>(uid, out var activeInvisible) ? activeInvisible.Opacity : 1;
            sprite.Color = Color.Transparent.WithAlpha(opacity);
        }
    }
}
