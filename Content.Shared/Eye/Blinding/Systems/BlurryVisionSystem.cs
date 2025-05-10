using System.Linq;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared._RMC14.Medical.TemporaryBlurryVision;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlurryVisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionCorrectionComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<VisionCorrectionComponent, GotUnequippedEvent>(OnGlassesUnequipped);
        SubscribeLocalEvent<VisionCorrectionComponent, InventoryRelayedEvent<GetBlurEvent>>(OnGetBlur);
    }

    private void OnGetBlur(Entity<VisionCorrectionComponent> glasses, ref InventoryRelayedEvent<GetBlurEvent> args)
    {
        args.Args.Blur += glasses.Comp.VisionBonus;
        args.Args.CorrectionPower *= glasses.Comp.CorrectionPower;
    }

    public void UpdateBlurMagnitude(Entity<BlindableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var maxModificatorStrength = 0;

        if (TryComp<TemporaryBlurryVisionComponent>(ent, out var comp))
        {
            if (comp.TemporaryBlurModificators.Count != 0)
            {
                maxModificatorStrength = comp.TemporaryBlurModificators.Max(mod => mod.EffectStrength);
            }
        }

        var ev = new GetBlurEvent(Math.Max(ent.Comp.EyeDamage, maxModificatorStrength));
        RaiseLocalEvent(ent, ev);

        var blur = Math.Clamp(ev.Blur, 0, BlurryVisionComponent.MaxMagnitude);
        if (blur <= 0)
        {
            RemCompDeferred<BlurryVisionComponent>(ent);
            return;
        }

        var blurry = EnsureComp<BlurryVisionComponent>(ent);
        blurry.Magnitude = blur;
        blurry.CorrectionPower = ev.CorrectionPower;
        Dirty(ent, blurry);
    }

    private void OnGlassesEquipped(Entity<VisionCorrectionComponent> glasses, ref GotEquippedEvent args)
    {
        UpdateBlurMagnitude(args.Equipee);
    }

    private void OnGlassesUnequipped(Entity<VisionCorrectionComponent> glasses, ref GotUnequippedEvent args)
    {
        UpdateBlurMagnitude(args.Equipee);
    }
}

public sealed class GetBlurEvent : EntityEventArgs, IInventoryRelayEvent
{
    public readonly float BaseBlur;
    public float Blur;
    public float CorrectionPower = BlurryVisionComponent.DefaultCorrectionPower;

    public GetBlurEvent(float blur)
    {
        Blur = blur;
        BaseBlur = blur;
    }

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES;
}
