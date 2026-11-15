using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Medical.TemporaryBlurryVision;

public sealed class TemporaryBlurryVisionSystem : EntitySystem
{
    [Dependency] private readonly BlurryVisionSystem _blur = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, GetBlurEvent>(OnGetBlur);
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public void AddTemporaryBlurModifier(Entity<TemporaryBlurryVisionComponent?> ent, TimeSpan duration, int strength)
    {
        var mod = new TemporaryBlurModifier(duration + _timing.CurTime, strength);
        AddTemporaryBlurModifier(ent, mod);
    }

    public void AddTemporaryBlurModifier(Entity<TemporaryBlurryVisionComponent?> ent, TemporaryBlurModifier mod)
    {
        ent.Comp = EnsureComp<TemporaryBlurryVisionComponent>(ent);
        ent.Comp.TemporaryBlurModifiers.Add(mod);
        Dirty(ent, ent.Comp);

        _blur.UpdateBlurMagnitude(ent.Owner);
    }

    private void OnGetBlur(Entity<TemporaryBlurryVisionComponent> ent, ref GetBlurEvent args)
    {
        if (ent.Comp.TemporaryBlurModifiers.Count == 0)
            return;

        args.Blur = ent.Comp.TemporaryBlurModifiers.Max(mod => mod.EffectStrength);
    }

    private void OnRejuvenate(Entity<TemporaryBlurryVisionComponent> ent, ref RejuvenateEvent args)
    {
        RemComp<TemporaryBlurryVisionComponent>(ent);
        _blur.UpdateBlurMagnitude(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var blurQuery = EntityQueryEnumerator<TemporaryBlurryVisionComponent>();
        while (blurQuery.MoveNext(out var uid, out var blur))
        {
            if (time < blur.NextUpdateTime)
                continue;
            blur.NextUpdateTime = time + blur.UpdateRate;

            if (blur.TemporaryBlurModifiers.RemoveAll(mod => time > mod.ExpireAt) != 0)
                _blur.UpdateBlurMagnitude(uid);

            Dirty(uid, blur);
        }
    }
}
