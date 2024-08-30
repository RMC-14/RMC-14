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
        _appearance.SetData(ent, SpriteSetRenderOrderComponent.Appearance.Key, ent.Comp.RenderOrder);
    }
}
