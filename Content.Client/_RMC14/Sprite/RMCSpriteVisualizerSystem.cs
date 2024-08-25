using Content.Client.Outline;
using Content.Shared._RMC14.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteVisualizerSystem : VisualizerSystem<SpriteSetRenderOrderComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(InteractionOutlineSystem));
    }

    protected override void OnAppearanceChange(EntityUid uid, SpriteSetRenderOrderComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, SpriteSetRenderOrderComponent.Appearance.Key, out int order, args.Component))
            args.Sprite.RenderOrder = (uint) order;
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<SpriteSetRenderOrderComponent, SpriteComponent>();
        while (query.MoveNext(out var set, out var sprite))
        {
            sprite.RenderOrder = (uint) set.RenderOrder;
        }
    }
}
