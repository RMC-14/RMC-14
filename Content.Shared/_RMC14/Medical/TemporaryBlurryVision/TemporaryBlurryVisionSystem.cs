using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Medical.TemporaryBlurryVision;

public sealed class TemporaryBlurryVisionSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]

    [Dependency] private readonly BlurryVisionSystem _blur = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, GetBlurEvent>(OnGetBlur);
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public void AddTemporaryBlurModificator(EntityUid uid, TimeSpan duration, int strength, TemporaryBlurryVisionComponent? blur = null)
    {
        var mod = new TemporaryBlurModificator(duration + _timing.CurTime, strength);
        AddTemporaryBlurModificator(uid, mod, blur);
    }
    public void AddTemporaryBlurModificator(EntityUid uid, TemporaryBlurModificator mod, TemporaryBlurryVisionComponent? blur = null)
    {
        blur = EnsureComp<TemporaryBlurryVisionComponent>(uid);

        blur.TemporaryBlurModificators.Add(mod);
        _blur.UpdateBlurMagnitude(uid);
        Dirty(uid, blur);
    }

    private void OnGetBlur(EntityUid uid, TemporaryBlurryVisionComponent comp, ref GetBlurEvent args)
    {
        if (comp.TemporaryBlurModificators.Count == 0)
            return;

        args.Blur = comp.TemporaryBlurModificators.Max(mod => mod.EffectStrength);
    }

    private void OnRejuvenate(EntityUid uid, TemporaryBlurryVisionComponent comp, ref RejuvenateEvent args)
    {
        RemComp<TemporaryBlurryVisionComponent>(uid);
        _blur.UpdateBlurMagnitude(uid);
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

            if (blur.TemporaryBlurModificators.Any(mod => time > mod.ExpireAt))
            {
                blur.TemporaryBlurModificators.RemoveAll(mod => _timing.CurTime > mod.ExpireAt);
                _blur.UpdateBlurMagnitude(uid);
            }

            Dirty(uid, blur);
        }
    }
}
