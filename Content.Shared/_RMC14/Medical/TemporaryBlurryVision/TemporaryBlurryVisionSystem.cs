using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Medical.TemporaryBlurryVision;

public sealed class TemporaryBlurrySystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string TemporaryBlurryVisionKey = "TemporaryBlurryVision";

    [Dependency] private readonly BlurryVisionSystem _blur = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryBlurryVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, GetBlurEvent>(OnBlurUpdate);
    }

    private void OnStartup(EntityUid uid, TemporaryBlurryVisionComponent component, ComponentStartup args)
    {
        _blur.UpdateBlurMagnitude(uid);
    }

    private void OnShutdown(EntityUid uid, TemporaryBlurryVisionComponent component, ComponentShutdown args)
    {
        _blur.UpdateBlurMagnitude(uid);
    }

    private void OnBlurUpdate(EntityUid uid, TemporaryBlurryVisionComponent component, GetBlurEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Blur = Math.Max(component.Blur, args.Blur);
    }

    public void TryApplyBlindness(EntityUid uid, float blur, float seconds, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, TemporaryBlurryVisionKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<TemporaryBlurryVisionComponent>(uid, TemporaryBlurryVisionKey, TimeSpan.FromSeconds(seconds), true, status);
            if (TryComp<TemporaryBlurryVisionComponent>(uid, out var comp))
                comp.Blur = blur;
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, TemporaryBlurryVisionKey, TimeSpan.FromSeconds(seconds), status);
            if (TryComp<TemporaryBlurryVisionComponent>(uid, out var comp))
                comp.Blur = blur;
        }
    }
}
