using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Hide;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._CM14.Xenos.Hide;

public sealed class XenoHideVisualizerSystem : VisualizerSystem<XenoHideComponent>
{
    [Dependency] private readonly XenoVisualizerSystem _xenoVisualizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoHideComponent, GetDrawDepthEvent>(OnXenoHideGetDrawDepth,
            before: new[] { typeof(XenoVisualizerSystem) });
    }

    private void OnXenoHideGetDrawDepth(Entity<XenoHideComponent> ent, ref GetDrawDepthEvent args)
    {
        if (AppearanceSystem.TryGetData(ent, XenoVisualLayers.Hide, out bool hiding) &&
            hiding)
        {
            args.DrawDepth = DrawDepth.SmallMobs;
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoHideComponent component, ref AppearanceChangeEvent args)
    {
        _xenoVisualizer.UpdateDrawDepth((uid, args.Sprite));
    }
}
