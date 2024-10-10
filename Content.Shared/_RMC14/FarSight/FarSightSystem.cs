using Content.Shared._RMC14.Scoping;
using Content.Shared._RMC14.Overwatch;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.FarSight;

public sealed class FarSightSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
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

        // No farsight while watching through the console
        if (HasComp<OverwatchWatchingComponent>(args.User))
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnFarSightAction(Entity<FarSightItemComponent> ent, ref FarSightActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        // No farsight while watching through the console
        if (HasComp<ScopingComponent>(user) ||
            HasComp<OverwatchWatchingComponent>(user))
            return;

        args.Handled = true;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        SetZoom(ent.Comp.Enabled, user, ent);

        _actions.SetToggled(ent.Comp.Action, ent.Comp.Enabled);
        _appearance.SetData(ent, FarSightItemVisuals.Active, ent.Comp.Enabled);
    }

    private void OnFarSightEquipped(Entity<FarSightItemComponent> ent, ref GotEquippedEvent args)
    {
        var user = args.Equipee;

        if (!_inventory.InSlotWithFlags((ent, null, null), ent.Comp.Slots))
            return;

        if (HasComp<OverwatchWatchingComponent>(user))
            return;

        SetZoom(ent.Comp.Enabled, user, ent);
    }

    private void OnFarSightUnequipped(Entity<FarSightItemComponent> ent, ref GotUnequippedEvent args)
    {
        var user = args.Equipee;

        if (_inventory.InSlotWithFlags((ent, null, null), ent.Comp.Slots))
            return;

        SetZoom(false, user, ent);
    }

    public bool SetFarSightItem(EntityUid item, EntityUid user, bool state)
    {
        if (!TryComp(item, out FarSightItemComponent? comp))
            return false;

        SetZoom(state, user, (item, comp));
        return true;
    }

    private void SetZoom(bool activated, EntityUid user, Entity<FarSightItemComponent> item)
    {
        if (activated)
        {
            _eye.SetMaxZoom(user, item.Comp.Zoom);
            _eye.SetZoom(user, item.Comp.Zoom);

            if (!_timing.ApplyingState)
            {
                // Give user component to be able to tell they're using farsight
                var farSight = EnsureComp<FarSightComponent>(user);
                farSight.Item = item.Owner;
                Dirty(user, farSight);
            }
        }
        else
        {
            if (TryComp<EyeComponent>(user, out var eye))
                _eye.SetMaxZoom(user, eye.Zoom);

            if (TryComp(user, out FarSightComponent? farSight))
                RemCompDeferred<FarSightComponent>(user);

            _eye.ResetZoom(user);
        }
    }
}
