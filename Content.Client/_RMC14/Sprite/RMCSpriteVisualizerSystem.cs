using System.Numerics;
using Content.Client.Outline;
using Content.Shared._RMC14.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteVisualizerSystem : VisualizerSystem<SpriteSetRenderOrderComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        if (AppearanceSystem.TryGetData(uid, SpriteSetRenderOrderComponent.Appearance.Offset, out Vector2 offset, args.Component))
            _sprite.SetOffset((uid, args.Sprite), offset);
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = AllEntityQuery<SpriteSetRenderOrderComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var set, out var sprite))
        {
            if (set.RenderOrder != null)
                sprite.RenderOrder = (uint) set.RenderOrder.Value;

            if (set.Offset != null)
                _sprite.SetOffset((uid, sprite), set.Offset.Value);
        }
    }
}
