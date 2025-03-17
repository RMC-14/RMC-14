using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Medical.TemporaryBlurryVision;

public sealed class TemporaryBlurrySystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string BlurryVisionStatusEffect = "TemporaryBlurryVision";

    [Dependency] private readonly BlindableSystem _blindableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryBlurryVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TemporaryBlurryVisionComponent, CanSeeAttemptEvent>(OnBlindTrySee);
    }

    private void OnStartup(EntityUid uid, TemporaryBlurryVisionComponent component, ComponentStartup args)
    {
        _blindableSystem.UpdateIsBlind(uid);
    }

    private void OnShutdown(EntityUid uid, TemporaryBlurryVisionComponent component, ComponentShutdown args)
    {
        _blindableSystem.UpdateIsBlind(uid);
    }

    private void OnBlindTrySee(EntityUid uid, TemporaryBlurryVisionComponent component, CanSeeAttemptEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }
}
