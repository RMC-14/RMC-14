using Content.Shared._RMC14.Stun;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Xenonids.Tipping;

public sealed class RMCTippingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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

        var tippingDelay = GetTippingDelay(vendorTipComp, size.Size);
        var ev = new RMCTippingDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, tippingDelay, ev, ent.Owner, ent) { BreakOnMove = true };



        string leaningMessageLocID = "rmc-xeno-construction-vendor-tip-begin";
        _popup.PopupClient(Loc.GetString(leaningMessageLocID, ("vendorName", ent.Owner)), args.User, args.User);


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
        Log.Debug("ok");
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

        ent.Comp.IsTipped = true;

        _popup.PopupClient("you smahsed it mate", args.User, args.User);
        Log.Debug("completed do-after");
    }


}

