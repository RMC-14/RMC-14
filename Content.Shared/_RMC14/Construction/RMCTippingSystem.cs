using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.RatKing;

namespace Content.Shared._RMC14.Construction;

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
        Log.Debug("Starting check");

        if (!HasComp<XenoComponent>(args.User))
            return;

        var ev = new RMCTippingDoAfterEvent();
        Log.Debug("yea");
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent.Owner, ent) { BreakOnMove = true };
        Log.Debug("exploded");

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient("you begin to lean against it", ent, ent);
        }

        Log.Debug("yes you are a xeno interacting");

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
        Log.Debug("completed do-after");
    }


}

