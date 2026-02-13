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
        SubscribeLocalEvent<BuckleComponent, AfterAutoHandleStateEvent>(UpdateDrawDepth);
        SubscribeLocalEvent<StrapComponent, AfterAutoHandleStateEvent>(UpdateDrawDepth);

        SubscribeLocalEvent<RMCBuckleDrawDepthComponent, GetDrawDepthEvent>(OnGetDrawDepth, after: [typeof(XenoVisualizerSystem)]);
        SubscribeLocalEvent<RMCStrapDrawDepthComponent, GetDrawDepthEvent>(OnGetDrawDepth);
    }

    private void UpdateDrawDepth<T>(Entity<T> ent, ref AfterAutoHandleStateEvent args) where T : IComponent?
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

    private void OnGetDrawDepth(Entity<RMCStrapDrawDepthComponent> ent, ref GetDrawDepthEvent args)
    {
        if (TryComp(ent, out StrapComponent? strap) && strap.BuckledEntities.Count > 0)
            args.DrawDepth = ent.Comp.StrappedDepth;
        else
            args.DrawDepth = ent.Comp.UnstrappedDepth;
    }

    private int? GetDrawDepth(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap)
    {
        var buckleDepth = (int?) CompOrNull<RMCBuckleDrawDepthComponent>(buckle)?.BuckleDepth;

        if (!TryComp(strap, out SpriteComponent? strapSprite))
            return null;

        if (HasComp<RMCStrapNoDrawDepthChangeComponent>(strap) &&
            buckleDepth == null)
        {
            buckleDepth = strapSprite.DrawDepth + 1;
        }

        if (buckleDepth == null)
        {
            var isNorth = Transform(strap).LocalRotation.GetCardinalDir() == Direction.North;
            if (isNorth)
                buckleDepth = strapSprite.DrawDepth - 1;
            else
                buckleDepth = strapSprite.DrawDepth + 1;
        }

        return buckleDepth;
    }
}
