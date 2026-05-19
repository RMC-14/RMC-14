using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Invisibility;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Invisibility;

public sealed class XenoInvisibilityVisualsSystem : EntitySystem
{
    private EntityQuery<XenoActiveInvisibleComponent> _activeInvisibleQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        _activeInvisibleQuery = GetEntityQuery<XenoActiveInvisibleComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<XenoTurnInvisibleComponent, ComponentShutdown>(OnTurnInvisibleShutdown);
    }

    public override void Update(float frameTime)
    {
        var invisible = EntityQueryEnumerator<XenoTurnInvisibleComponent, SpriteComponent>();
        while (invisible.MoveNext(out var uid, out var comp, out var sprite))
        {
            var opacity = _activeInvisibleQuery.HasComp(uid) ? comp.Opacity : 1f;
            sprite.Color = Color.Transparent.WithAlpha(opacity);
        }
    }

    private void OnTurnInvisibleShutdown(Entity<XenoTurnInvisibleComponent> entity, ref ComponentShutdown args)
    {
        if(_spriteQuery.TryComp(entity, out var result) && result is SpriteComponent { } sprite)
        {
            sprite.Color = Color.Transparent.WithAlpha(1f);
        }
    }
}
