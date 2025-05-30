using Content.Client._RMC14.Sprite;
using Content.Client._RMC14.Xenonids;
using Content.Shared._RMC14.Buckle;
using Content.Shared._RMC14.Sprite;
using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Buckle;

public sealed class RMCBuckleVisualsSystem : EntitySystem
{
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleComponent, AfterAutoHandleStateEvent>(OnBuckleState);
        SubscribeLocalEvent<RMCBuckleDrawDepthComponent, GetDrawDepthEvent>(OnGetDrawDepth, after: [typeof(XenoVisualizerSystem)]);
    }

    private void OnBuckleState(Entity<BuckleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcSprite.UpdateDrawDepth(ent.Owner);
    }

    private void OnGetDrawDepth(Entity<RMCBuckleDrawDepthComponent> ent, ref GetDrawDepthEvent args)
    {
        if (TryComp(ent, out BuckleComponent? buckle) &&
            TryComp(buckle.BuckledTo, out StrapComponent? strap) &&
            GetDrawDepth((ent, buckle), (buckle.BuckledTo.Value, strap)) is { } drawDepth)
        {
            args.DrawDepth = (DrawDepth) drawDepth;
        }
    }

    private int? GetDrawDepth(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap)
    {
        var buckleDepth = (int?) CompOrNull<RMCBuckleDrawDepthComponent>(buckle)?.BuckleDepth;
        if (HasComp<RMCStrapNoDrawDepthChangeComponent>(strap) &&
            buckleDepth == null)
        {
            return null;
        }

        if (!TryComp(strap, out SpriteComponent? strapSprite))
            return null;

        if (buckleDepth == null)
        {
            var isNorth = Transform(strap).LocalRotation.GetCardinalDir() == Direction.North;
            if (isNorth)
                buckleDepth = strapSprite.DrawDepth - 1;
        }

        return buckleDepth;
    }
}
