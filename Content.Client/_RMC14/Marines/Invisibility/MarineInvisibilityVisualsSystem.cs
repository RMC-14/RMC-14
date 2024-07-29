using Content.Shared._RMC14.Marines.Invisibility;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Marines.Invisibility;

public sealed class MarineInvisibilityVisualsSystem : EntitySystem
{
    private EntityQuery<MarineActiveInvisibleComponent> _activeInvisibleQuery;
    public override void Initialize()
    {
        _activeInvisibleQuery = GetEntityQuery<MarineActiveInvisibleComponent>();
    }

    public override void Update(float frameTime)
    {
        var invisible = EntityQueryEnumerator<MarineTurnInvisibleComponent, SpriteComponent>();
        while (invisible.MoveNext(out var uid, out var comp, out var sprite))
        {
            var opacity =  _activeInvisibleQuery.HasComp(uid) ? comp.Opacity : 1;
            sprite.Color = Color.Transparent.WithAlpha(opacity);
        }
    }
}
