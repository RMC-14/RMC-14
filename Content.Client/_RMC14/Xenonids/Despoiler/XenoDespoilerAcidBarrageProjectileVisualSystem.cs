using Content.Shared._RMC14.Xenonids.Despoiler;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerAcidBarrageProjectileVisualSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDespoilerAcidBarrageProjectileComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<XenoDespoilerAcidBarrageProjectileComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnStartup(EntityUid uid, XenoDespoilerAcidBarrageProjectileComponent comp, ComponentStartup args)
    {
        Apply(uid, comp);
    }

    private void OnState(EntityUid uid, XenoDespoilerAcidBarrageProjectileComponent comp, ref AfterAutoHandleStateEvent args)
    {
        Apply(uid, comp);
    }

    private void Apply(EntityUid uid, XenoDespoilerAcidBarrageProjectileComponent comp)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        _sprite.SetScale((uid, sprite), comp.Scale);
    }
}
