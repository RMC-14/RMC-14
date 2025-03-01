using Content.Client._RMC14.Xenonids;
using Content.Client.Buckle;
using Content.Shared._RMC14.Buckle;
using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Buckle;

public sealed class RMCBuckleVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleComponent, BuckledEvent>(OnBuckled, after: [typeof(BuckleSystem)]);
        SubscribeLocalEvent<BuckleComponent, UnbuckledEvent>(OnUnbuckled, after: [typeof(BuckleSystem)]);

        SubscribeLocalEvent<RMCBuckleDrawDepthComponent, GetDrawDepthEvent>(OnGetDrawDepth, after: [typeof(XenoVisualizerSystem)]);
    }

    private void OnBuckled(Entity<BuckleComponent> ent, ref BuckledEvent args)
    {
        SetBuckleDrawDepth(args.Buckle, args.Strap);
    }

    private void OnUnbuckled(Entity<BuckleComponent> ent, ref UnbuckledEvent args)
    {
        if (ent.Comp.OriginalDrawDepth is not { } drawDepth)
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        if (TryComp(ent, out RMCBuckleDrawDepthComponent? drawDepthComp))
            drawDepth = (int) drawDepthComp.UnbuckleDepth;

        sprite.DrawDepth = drawDepth;

#pragma warning disable RA0002
        ent.Comp.OriginalDrawDepth = null;
#pragma warning restore RA0002
        Dirty(ent);
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

    private void SetBuckleDrawDepth(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap)
    {
        var buckleDepth = GetDrawDepth(buckle, strap);
        if (!TryComp(buckle, out SpriteComponent? buckledSprite) ||
            !TryComp(strap, out SpriteComponent? strapSprite))
        {
            return;
        }

        if (buckleDepth != null)
        {
            buckledSprite.DrawDepth = buckleDepth.Value;

#pragma warning disable RA0002
            buckle.Comp.OriginalDrawDepth = strapSprite.DrawDepth;
#pragma warning restore RA0002
            Dirty(buckle);
        }
    }
}
