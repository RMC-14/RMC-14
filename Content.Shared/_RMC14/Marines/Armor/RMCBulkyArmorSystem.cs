using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Marines.Armor;

public sealed class RMCBulkyArmorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBulkyArmorComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
    }

    private void OnBeingEquippedAttempt(Entity<RMCBulkyArmorComponent> armor, ref BeingEquippedAttemptEvent args)
    {
        if (!armor.Comp.IsBulky || !HasComp<RMCUserBulkyArmorIncapableComponent>(args.EquipTarget))
            return;

        if (args.EquipTarget == args.Equipee)
            _popup.PopupClient(Loc.GetString("rmc-bulky-armor-user-unable", ("armor", armor)), args.Equipee, args.Equipee, PopupType.MediumCaution);
        else
            _popup.PopupEntity(Loc.GetString("rmc-bulky-armor-target-unable", ("target", args.EquipTarget), ("armor", armor)), args.Equipee, args.Equipee, PopupType.MediumCaution);

        args.Cancel();
    }
}
