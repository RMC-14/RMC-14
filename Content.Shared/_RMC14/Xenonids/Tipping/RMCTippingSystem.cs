using Content.Shared._RMC14.Stun;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;

namespace Content.Shared._RMC14.Xenonids.Tipping;

public sealed class RMCTippingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedRotationVisualsSystem _rotationVisuals = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<RMCTippableComponent, InteractHandEvent>(TippableInteractHand);
        SubscribeLocalEvent<RMCTippableComponent, DoAfterAttemptEvent<RMCTippingDoAfterEvent>>(OnTippingDoAfterAttempt);
        SubscribeLocalEvent<RMCTippableComponent, RMCTippingDoAfterEvent>(OnTippingDoAfter);

    }

    private void TippableInteractHand(Entity<RMCTippableComponent> ent, ref InteractHandEvent args)
    {
        if (!HasComp<XenoComponent>(args.User))
            return;

        if (!TryComp(args.User, out VendorTipTimeComponent? vendorTipComp))
            return;

        if (!TryComp(args.User, out RMCSizeComponent? size))
            return;

        args.Handled = true;

        if (ent.Comp.IsTipped)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-construction-vendor-tip-done"), args.User, args.User);
            return;
        }

        var tippingDelay = GetTippingDelay(vendorTipComp, size.Size);
        var ev = new RMCTippingDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, tippingDelay, ev, ent.Owner, ent) { BreakOnMove = true };

        _popup.PopupClient(Loc.GetString("rmc-xeno-construction-vendor-tip-begin", ("vendorName", ent.Owner)), args.User, args.User);
        _doAfter.TryStartDoAfter(doAfter);
    }

    private TimeSpan GetTippingDelay(VendorTipTimeComponent vendorTipComp, RMCSizes size)
    {
        if (vendorTipComp.IsCrusher)
            return vendorTipComp.CrusherDelay;

        if (size >= RMCSizes.Big)
            return vendorTipComp.BigDelay;

        return vendorTipComp.DefaultDelay;
    }
    private void OnTippingDoAfterAttempt(Entity<RMCTippableComponent> ent, ref DoAfterAttemptEvent<RMCTippingDoAfterEvent> args)
    {
        if (args.Event.Target is { } action &&
            TryComp(action, out InstantActionComponent? actionComponent) &&
            !actionComponent.Enabled)
        {
            args.Cancel();
        }
    }

    private void OnTippingDoAfter(Entity<RMCTippableComponent> ent, ref RMCTippingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var time = TimeSpan.FromMinutes(5);

        ent.Comp.IsTipped = true;
        _standingState.Down(ent.Owner);
        var comp = AddComp<RotationVisualsComponent>(ent.Owner);
        comp.VerticalRotation = 120;
        _rotationVisuals.SetHorizontalAngle(ent.Owner, 30);
        comp.HorizontalRotation = 30;
        _rotationVisuals.SetHorizontalAngle(ent.Owner, 30);
        Log.Debug("after down state");

        _statusEffect.TryAddStatusEffect<KnockedDownComponent>(ent.Owner, "KnockedDown", time, true);

        _popup.PopupClient(Loc.GetString("rmc-xeno-construction-vendor-tip-success", ("vendorName", ent.Owner)), args.User, args.User);
    }
}

