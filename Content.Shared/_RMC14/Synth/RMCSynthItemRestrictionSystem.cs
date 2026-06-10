using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Synth;

/// <summary>
/// Centralizes synthetic item restrictions so item prototypes do not need custom role checks.
/// </summary>
public sealed class RMCSynthItemRestrictionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSynthItemRestrictionComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RMCSynthItemRestrictionComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<RMCSynthItemRestrictionComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCSynthItemRestrictionComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<RMCSynthItemRestrictionComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RMCSynthItemRestrictionComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
    }

    private void OnPickupAttempt(Entity<RMCSynthItemRestrictionComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!ent.Comp.CheckPickup || IsAllowed(ent.Comp, args.User))
            return;

        args.Cancel();
        Popup(ent, args.User);
    }

    private void OnEquipAttempt(Entity<RMCSynthItemRestrictionComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.CheckEquip || IsAllowed(ent.Comp, args.EquipTarget))
            return;

        args.Cancel();
        args.Reason = ent.Comp.DenyPopup;
    }

    private void OnUseInHand(Entity<RMCSynthItemRestrictionComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.CheckUse || IsAllowed(ent.Comp, args.User))
            return;

        args.Handled = true;
        Popup(ent, args.User);
    }

    private void OnActivate(Entity<RMCSynthItemRestrictionComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !ent.Comp.CheckUse || IsAllowed(ent.Comp, args.User))
            return;

        args.Handled = true;
        Popup(ent, args.User);
    }

    private void OnAfterInteract(Entity<RMCSynthItemRestrictionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !ent.Comp.CheckUse || IsAllowed(ent.Comp, args.User))
            return;

        args.Handled = true;
        Popup(ent, args.User);
    }

    private void OnBeforeRangedInteract(Entity<RMCSynthItemRestrictionComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !ent.Comp.CheckUse || IsAllowed(ent.Comp, args.User))
            return;

        args.Handled = true;
        Popup(ent, args.User);
    }

    private bool IsAllowed(RMCSynthItemRestrictionComponent comp, EntityUid user)
    {
        // SynthOnly=false is used for items that should specifically reject synths.
        return comp.SynthOnly == HasComp<SynthComponent>(user);
    }

    private void Popup(Entity<RMCSynthItemRestrictionComponent> ent, EntityUid user)
    {
        _popup.PopupClient(Loc.GetString(ent.Comp.DenyPopup, ("item", ent.Owner), ("user", user)), user, user, PopupType.SmallCaution);
    }
}
