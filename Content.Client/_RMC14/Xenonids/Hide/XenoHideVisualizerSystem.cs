using Content.Client._RMC14.Sprite;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hide;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Xenonids.Hide;

public sealed class XenoHideVisualizerSystem : VisualizerSystem<XenoHideComponent>
{
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoHideComponent, GetDrawDepthEvent>(OnXenoHideGetDrawDepth,
            before: [typeof(XenoVisualizerSystem)]);
    }

    private void OnXenoHideGetDrawDepth(Entity<XenoHideComponent> ent, ref GetDrawDepthEvent args)
    {
        if (AppearanceSystem.TryGetData(ent, XenoVisualLayers.Hide, out bool hiding) &&
            hiding)
        {
            args.DrawDepth = DrawDepth.Walls;
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoHideComponent component, ref AppearanceChangeEvent args)
    {
        _rmcSprite.UpdateDrawDepth(uid);
    }
}
