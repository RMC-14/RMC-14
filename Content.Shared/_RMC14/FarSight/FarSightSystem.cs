using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.FarSight;

public sealed class FarSightSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FarSightItemComponent, GetItemActionsEvent>(OnFarSightGetItemActions);
        SubscribeLocalEvent<FarSightItemComponent, FarSightActionEvent>(OnFarSightAction);
        SubscribeLocalEvent<FarSightItemComponent, GotUnequippedEvent>(OnFarSightUnequipped);
        SubscribeLocalEvent<FarSightItemComponent, GotEquippedEvent>(OnFarSightEquipped);
    }

    private void OnFarSightGetItemActions(Entity<FarSightItemComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands || !_inventory.InSlotWithFlags((ent, null, null), ent.Comp.Slots))
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnFarSightAction(Entity<FarSightItemComponent> ent, ref FarSightActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        var user = args.Performer;
        SetZoom(ent.Comp.Enabled, user, ent.Comp);

        _actions.SetToggled(ent.Comp.Action, ent.Comp.Enabled);
        _appearance.SetData(ent, FarSightItemVisuals.Active, ent.Comp.Enabled);
    }

    private void OnFarSightEquipped(Entity<FarSightItemComponent> ent, ref GotEquippedEvent args)
    {
        var user = args.Equipee;

        if (!_inventory.InSlotWithFlags((ent, null, null), ent.Comp.Slots))
            return;

        SetZoom(ent.Comp.Enabled, user, ent.Comp);
    }

    private void OnFarSightUnequipped(Entity<FarSightItemComponent> ent, ref GotUnequippedEvent args)
    {
        var user = args.Equipee;

        if (_inventory.InSlotWithFlags((ent, null, null), ent.Comp.Slots))
            return;

        SetZoom(false, user, ent.Comp);
    }

    private void SetZoom(bool activated, EntityUid user, FarSightItemComponent comp)
    {
        if (activated)
        {
            _eye.SetMaxZoom(user, comp.Zoom);
            _eye.SetZoom(user, comp.Zoom);
        }
        else
        {
            if (TryComp<EyeComponent>(user, out var eye))
                _eye.SetMaxZoom(user, eye.Zoom);

            _eye.ResetZoom(user);
        }
    }
}
