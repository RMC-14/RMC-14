using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hide;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Infected;

public sealed class XenoParasiteSystem : SharedXenoParasiteSystem
{
    [Dependency] private readonly XenoVisualizerSystem _xenoVisualizer = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoParasiteComponent, GetDrawDepthEvent>(OnGetParasiteDrawDepth, before: [typeof(XenoHideSystem)]);
    }

    private void OnGetParasiteDrawDepth(Entity<XenoParasiteComponent> parasite, ref GetDrawDepthEvent args)
    {
        if (_tags.HasTag(parasite, ParasiteIsPreparingLeapProtoID) ||
            HasComp<XenoLeapingComponent>(parasite))
        {
            args.DrawDepth = Shared.DrawDepth.DrawDepth.Overdoors;
        }
        else
        {
            args.DrawDepth = Shared.DrawDepth.DrawDepth.Mobs;
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<XenoComponent, ThrownItemComponent, SpriteComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var thrown, out var sprite, out var appearance))
        {
            _xenoVisualizer.UpdateSprite((uid, sprite, null, appearance, null, thrown));
        }
    }
}
