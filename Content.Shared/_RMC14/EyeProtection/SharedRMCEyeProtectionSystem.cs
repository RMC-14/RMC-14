using Content.Shared._RMC14.SightRestriction;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Rounding;
using Content.Shared.StatusEffect;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;

using Robust.Shared.Timing;

namespace Content.Shared._RMC14.EyeProtection
{
    public abstract class SharedRMCEyeProtectionSystem : EntitySystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly BlindableSystem _blindingSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ClothingSystem _clothingSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();


            SubscribeLocalEvent<RMCEyeProtectionItemComponent, ToggleClothingCheckEvent>(OnEyeProtectionItemToggleCheck);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, ToggleActionEvent>(OnEyeProtectionItemToggle);

            SubscribeLocalEvent<RMCEyeProtectionItemComponent, GotEquippedEvent>(OnEyeProtectionItemGotEquipped);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, GotUnequippedEvent>(OnEyeProtectionItemGotUnequipped);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, ActionRemovedEvent>(OnEyeProtectionItemActionRemoved);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, ComponentRemove>(OnEyeProtectionItemRemove);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, EntityTerminatingEvent>(OnEyeProtectionItemTerminating);
        }

        private void OnEyeProtectionItemToggleCheck(Entity<RMCEyeProtectionItemComponent> item, ref ToggleClothingCheckEvent args)
        {
            // Get containing slot and check if item in proper slot
            if (!_inventory.TryGetContainingSlot((item.Owner, null, null), out var slotDef) ||
                slotDef.SlotFlags != item.Comp.Slots)
                args.Cancelled = true;
        }

        private void OnEyeProtectionItemToggle(Entity<RMCEyeProtectionItemComponent> item, ref ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            ToggleEyeProtectionItem(item, args.Performer);
        }

        private void UpdateEquippedSprite(Entity<RMCEyeProtectionItemComponent> item)
        {
            if (!TryComp<ClothingComponent>(item.Owner, out var clothing))
                return;

            if (item.Comp.RaisedEquippedPrefix is not { } prefix)
                return;

            // Update sprite
            _clothingSystem.SetEquippedPrefix(item.Owner, item.Comp.Toggled ? prefix : null, clothing);
        }

        private void EquippedPopup(Entity<RMCEyeProtectionItemComponent> item)
        {
            if (!item.Comp.Toggleable)
                return;

            string msg;
            if (item.Comp.PopupName is not { } popup)
            {
                msg = Loc.GetString(item.Comp.Toggled
                    ? "rmc-weld-protection-down"
                    : "rmc-weld-protection-up",
                    ("protection",  item.Owner));
            }
            else
            {
                msg = Loc.GetString(item.Comp.Toggled
                    ? "rmc-weld-protection-down"
                    : "rmc-weld-protection-up",
                    ("protection",  popup));
            }

            _popup.PopupClient(msg, item.Owner, item.Owner);
        }

        private void OnEyeProtectionItemGotEquipped(Entity<RMCEyeProtectionItemComponent> item, ref GotEquippedEvent args)
        {
            if (item.Comp.Slots != args.SlotFlags)
                return;

            UpdateEquippedSprite(item);
        }

        private void OnEyeProtectionItemGotUnequipped(Entity<RMCEyeProtectionItemComponent> item, ref GotUnequippedEvent args)
        {
            if (item.Comp.Slots != args.SlotFlags)
                return;

            DisableEyeProtectionItem(item, args.Equipee);
        }

        private void OnEyeProtectionItemActionRemoved(Entity<RMCEyeProtectionItemComponent> item, ref ActionRemovedEvent args)
        {
            DisableEyeProtectionItem(item, item.Comp.User);
        }

        private void OnEyeProtectionItemRemove(Entity<RMCEyeProtectionItemComponent> item, ref ComponentRemove args)
        {
            DisableEyeProtectionItem(item, item.Comp.User);
        }

        private void OnEyeProtectionItemTerminating(Entity<RMCEyeProtectionItemComponent> item, ref EntityTerminatingEvent args)
        {
            DisableEyeProtectionItem(item, item.Comp.User);
        }

        private void EnableEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid user)
        {
            // Check if already enabled
            if (item.Comp.Toggled)
                return;

            item.Comp.User = user;
            item.Comp.Toggled = true;

            // Update icon
            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, true);

            Dirty(item);
        }

        protected void DisableEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid? user)
        {
            item.Comp.User = null;
            item.Comp.Toggled = false;

            // Update icon
            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, false);

            Dirty(item);
        }

        private void ToggleEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid user)
        {
            if (!item.Comp.Toggleable)
                return;

            if (item.Comp.Toggled)
                DisableEyeProtectionItem(item, item.Comp.User);
            else
                EnableEyeProtectionItem(item, user);

            EquippedPopup(item);
            UpdateEquippedSprite(item);
        }
    }
}
