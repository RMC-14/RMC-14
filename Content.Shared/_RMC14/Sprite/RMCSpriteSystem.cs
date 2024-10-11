namespace Content.Shared._RMC14.Sprite;

public sealed class RMCSpriteSystem : EntitySystem
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
}
