using Content.Shared._RMC14.Xenonids.Despoiler.Barrage;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerAcidBarrageProjectileVisualSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDespoilerAcidBarrageProjectileComponent, ComponentStartup>(OnApply);
        SubscribeLocalEvent<XenoDespoilerAcidBarrageProjectileComponent, AfterAutoHandleStateEvent>(OnApply);
    }

    private void OnApply<T>(Entity<XenoDespoilerAcidBarrageProjectileComponent> ent, ref T args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetScale((ent, sprite), ent.Comp.Scale);
    }
}
