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
    public abstract class RMCSharedEyeProtectionSystem : EntitySystem
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
            SubscribeLocalEvent<RMCRequiresEyeProtectionComponent, ToolUseAttemptEvent>(OnUseAttempt);
            SubscribeLocalEvent<RMCRequiresEyeProtectionComponent, ItemToggledEvent>(OnWelderToggled);

            SubscribeLocalEvent<RMCEyeProtectionItemComponent, GetEyeProtectionEvent>(OnGetProtection);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, InventoryRelayedEvent<GetEyeProtectionEvent>>(OnGetRelayedProtection);

            SubscribeLocalEvent<RMCEyeProtectionComponent, ComponentStartup>(OnEyeProtectionStartup);
            SubscribeLocalEvent<RMCEyeProtectionComponent, AfterAutoHandleStateEvent>(OnEyeProtectionAfterHandle);
            SubscribeLocalEvent<RMCEyeProtectionComponent, ComponentRemove>(OnEyeProtectionRemove);

            SubscribeLocalEvent<RMCEyeProtectionItemComponent, GetItemActionsEvent>(OnEyeProtectionItemGetActions);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, ToggleActionEvent>(OnEyeProtectionItemToggle);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, GotEquippedEvent>(OnEyeProtectionItemGotEquipped);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, GotUnequippedEvent>(OnEyeProtectionItemGotUnequipped);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, ActionRemovedEvent>(OnEyeProtectionItemActionRemoved);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, ComponentRemove>(OnEyeProtectionItemRemove);
            SubscribeLocalEvent<RMCEyeProtectionItemComponent, EntityTerminatingEvent>(OnEyeProtectionItemTerminating);
        }

        private void OnEyeProtectionStartup(Entity<RMCEyeProtectionComponent> ent, ref ComponentStartup args)
        {
            EyeProtectionChanged(ent);
        }

        private void OnEyeProtectionAfterHandle(Entity<RMCEyeProtectionComponent> ent, ref AfterAutoHandleStateEvent args)
        {
            EyeProtectionChanged(ent);
        }

        private void OnEyeProtectionRemove(Entity<RMCEyeProtectionComponent> ent, ref ComponentRemove args)
        {
            if (ent.Comp.Alert is { } alert)
                _alerts.ClearAlert(ent, alert);

            EyeProtectionRemoved(ent);
        }

        private void OnGetRelayedProtection(EntityUid uid, RMCEyeProtectionItemComponent component,
            InventoryRelayedEvent<GetEyeProtectionEvent> args)
        {
            OnGetProtection(uid, component, args.Args);
        }

        private void OnGetProtection(EntityUid uid, RMCEyeProtectionItemComponent component, GetEyeProtectionEvent args)
        {
            if (component.Toggled)
                args.Protection += component.ProtectionTime;
        }

        private void OnUseAttempt(EntityUid uid, RMCRequiresEyeProtectionComponent component, ToolUseAttemptEvent args)
        {
            if (!component.Toggled)
                return;

            if (!TryComp<BlindableComponent>(args.User, out var blindable) || blindable.IsBlind)
                return;

            var ev = new GetEyeProtectionEvent();
            RaiseLocalEvent(args.User, ev);

            var time = (float) (component.StatusEffectTime - ev.Protection).TotalSeconds;
            if (time <= 0)
                return;

            // Add permanent eye damage if they had zero protection, also somewhat scale their temporary blindness by
            // how much damage they already accumulated.
            _blindingSystem.AdjustEyeDamage((args.User, blindable), 1);
            var statusTimeSpan = TimeSpan.FromSeconds(time * MathF.Sqrt(blindable.EyeDamage));
            _statusEffectsSystem.TryAddStatusEffect(args.User, TemporaryBlindnessSystem.BlindingStatusEffect,
                statusTimeSpan, false, TemporaryBlindnessSystem.BlindingStatusEffect);
        }

        private void OnWelderToggled(EntityUid uid, RMCRequiresEyeProtectionComponent component, ItemToggledEvent args)
        {
            component.Toggled = args.Activated;
        }

        private void OnEyeProtectionItemGetActions(Entity<RMCEyeProtectionItemComponent> ent, ref GetItemActionsEvent args)
        {
            if (args.InHands || !ent.Comp.Toggleable)
                return;

            // Item not in a position for protecting eyes
            if (ent.Comp.Slots != args.SlotFlags)
                return;

            args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
            Dirty(ent);
        }

        private void OnEyeProtectionItemToggle(Entity<RMCEyeProtectionItemComponent> ent, ref ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            ToggleEyeProtectionItem(ent, args.Performer);
        }

        private void OnEyeProtectionItemGotEquipped(Entity<RMCEyeProtectionItemComponent> ent, ref GotEquippedEvent args)
        {
            // Item not in a position for protecting eyes
            if (ent.Comp.Slots != args.SlotFlags)
                return;

            if (!TryComp<ClothingComponent>(ent.Owner, out var clothingComp))
                return;

            // Display correct sprite, if applicable
            if (ent.Comp.RaisedEquippedPrefix != null)
                _clothingSystem.SetEquippedPrefix(ent.Owner, ent.Comp.RaisedEquippedPrefix, clothingComp);

            //EnableEyeProtectionItem(ent, args.Equipee);
        }

        private void OnEyeProtectionItemGotUnequipped(Entity<RMCEyeProtectionItemComponent> ent, ref GotUnequippedEvent args)
        {
            // Item not in a position for protecting eyes
            if (ent.Comp.Slots != args.SlotFlags)
                return;

            DisableEyeProtectionItem(ent, args.Equipee);
        }

        private void OnEyeProtectionItemActionRemoved(Entity<RMCEyeProtectionItemComponent> ent, ref ActionRemovedEvent args)
        {
            DisableEyeProtectionItem(ent, ent.Comp.User);
        }

        private void OnEyeProtectionItemRemove(Entity<RMCEyeProtectionItemComponent> ent, ref ComponentRemove args)
        {
            DisableEyeProtectionItem(ent, ent.Comp.User);
        }

        private void OnEyeProtectionItemTerminating(Entity<RMCEyeProtectionItemComponent> ent, ref EntityTerminatingEvent args)
        {
            DisableEyeProtectionItem(ent, ent.Comp.User);
        }

        private void EnableEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid user)
        {
            var ent = item.Owner;

            if (!TryComp<ClothingComponent>(ent, out var clothingComp))
                return;

            // Check if already enabled
            if (TryComp(user, out RMCEyeProtectionComponent? eyeProt))
            {
                RemComp<RMCEyeProtectionComponent>(user);
            }

            item.Comp.User = user;
            item.Comp.Toggled = true;

            // Display correct worn sprite
            if (item.Comp.RaisedEquippedPrefix != null)
            {
                _clothingSystem.SetEquippedPrefix(ent, null, clothingComp);
            }

            // Update icon
            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, true);

            // Update action
            _actions.SetToggled(item.Comp.Action, true);

            // Display pop-up
            if (item.Comp.PopupName != null)
            {
                var msg = Loc.GetString("rmc-weld-protection-down", ("protection", item.Comp.PopupName));
                _popup.PopupClient(msg, ent, user, PopupType.Small);
            }
            else
            {
                var msg = Loc.GetString("rmc-weld-protection-down", ("protection", ent));
                _popup.PopupClient(msg, ent, user, PopupType.Small);
            }

            Dirty(item);

            if (!_timing.ApplyingState)
            {
                eyeProt = EnsureComp<RMCEyeProtectionComponent>(user); // This should happen as late as possible
                Dirty(user, eyeProt);
            }
        }

        protected void DisableEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid? user)
        {
            var ent = item.Owner;

            if (!TryComp<ClothingComponent>(ent, out var clothingComp))
                return;

            // Can't disable what isn't there
            if (!TryComp(user, out RMCEyeProtectionComponent? eyeProt))
                return;

            // Display pop-up
            if (item.Comp.PopupName != null)
            {
                var msg = Loc.GetString("rmc-weld-protection-up", ("protection", item.Comp.PopupName));
                _popup.PopupClient(msg, ent, user, PopupType.Small);
            }
            else
            {
                var msg = Loc.GetString("rmc-weld-protection-up", ("protection", ent));
                _popup.PopupClient(msg, ent, user, PopupType.Small);
            }

            item.Comp.User = null;
            item.Comp.Toggled = false;

            // Display correct worn sprite, if applicable
            if (item.Comp.RaisedEquippedPrefix != null)
                _clothingSystem.SetEquippedPrefix(ent, item.Comp.RaisedEquippedPrefix, clothingComp);

            // Update icon
            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, false);

            // Update action
            _actions.SetToggled(item.Comp.Action, false);

            Dirty(item);

            RemComp<RMCEyeProtectionComponent>(user.Value);
        }

        private void ToggleEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid user)
        {
            if (!item.Comp.Toggleable)
                return;

            if (item.Comp.Toggled)
                DisableEyeProtectionItem(item, item.Comp.User);
            else
                EnableEyeProtectionItem(item, user);

            return;
        }

        protected virtual void EyeProtectionChanged(Entity<RMCEyeProtectionComponent> ent) { }

        protected virtual void EyeProtectionRemoved(Entity<RMCEyeProtectionComponent> ent) { }

    }
}
