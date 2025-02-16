using System.Numerics;

namespace Content.Shared._RMC14.Sprite;

public abstract class SharedRMCSpriteSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpriteSetRenderOrderComponent, MapInitEvent>(OnSetRenderOrderMapInit);
    }

    private void OnSetRenderOrderMapInit(Entity<SpriteSetRenderOrderComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.RenderOrder != null)
            _appearance.SetData(ent, SpriteSetRenderOrderComponent.Appearance.Key, ent.Comp.RenderOrder);

        if (ent.Comp.Offset != null)
            _appearance.SetData(ent, SpriteSetRenderOrderComponent.Appearance.Offset, ent.Comp.Offset);
    }

    public void SetOffset(EntityUid ent, Vector2 offset)
    {
        var sprite = EnsureComp<SpriteSetRenderOrderComponent>(ent);
        sprite.Offset = offset;
        Dirty(ent, sprite);
    }

    public void SetRenderOrder(EntityUid ent, int order)
    {
        var sprite = EnsureComp<SpriteSetRenderOrderComponent>(ent);
        sprite.RenderOrder = order;
        Dirty(ent, sprite);
    }

    public void SetColor(Entity<SpriteColorComponent?> ent, Color color)
    {
        ent.Comp = EnsureComp<SpriteColorComponent>(ent);
        ent.Comp.Color = color;
        Dirty(ent);
    }
}
