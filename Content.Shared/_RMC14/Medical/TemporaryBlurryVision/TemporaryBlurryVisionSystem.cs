using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.TemporaryBlurryVision;

public sealed class TemporaryBlurryVisionSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]

    [Dependency] private readonly BlurryVisionSystem _blur = default!;

    public void AddTemporaryBlurModificator(EntityUid uid, TemporaryBlurModificator mod, TemporaryBlurryVisionComponent? blur = null)
    {
        blur = EnsureComp<TemporaryBlurryVisionComponent>(uid);

        blur.TemporaryBlurModificators.Add(mod);
        _blur.UpdateBlurMagnitude(uid);
        Dirty(uid, blur);

        Timer.Spawn(mod.Duration, () => RemoveTemporaryBlurModificator(uid, mod, blur));
    }

    private void RemoveTemporaryBlurModificator(EntityUid uid, TemporaryBlurModificator mod, TemporaryBlurryVisionComponent? blur = null)
    {
        if (!Resolve(uid, ref blur))
            return;

        blur.TemporaryBlurModificators.Remove(mod);
        _blur.UpdateBlurMagnitude(uid);
        Dirty(uid, blur);
    }
}
