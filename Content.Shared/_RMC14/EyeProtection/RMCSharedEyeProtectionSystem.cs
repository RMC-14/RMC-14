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
        [Dependency] private readonly IEntityManager _entManager = default!;

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
            if (!(_inventory.InSlotWithFlags((ent,null,null), SlotFlags.MASK) ||
                    _inventory.InSlotWithFlags((ent,null,null), SlotFlags.EYES)))
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
            EnableEyeProtectionItem(ent, args.Equipee);
        }

        private void OnEyeProtectionItemGotUnequipped(Entity<RMCEyeProtectionItemComponent> ent, ref GotUnequippedEvent args)
        {
            // Item was not in a position for protecting eyes
            if ((args.SlotFlags != SlotFlags.MASK) && (args.SlotFlags != SlotFlags.EYES))
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
            if (!TryComp<ClothingComponent>(item.Owner, out var clothingComp) ||
                !TryComp<ItemComponent>(item.Owner, out var itemComp))
                return;

            // Check if item not in a position for protecting eyes
            if (!(_inventory.InSlotWithFlags((item,null,null), SlotFlags.MASK) ||
                _inventory.InSlotWithFlags((item,null,null), SlotFlags.EYES)))
                return;

            // Check if already enabled
            if (TryComp(user, out RMCEyeProtectionComponent? eyeProt))
                return;

            item.Comp.User = user;
            item.Comp.Toggled = true;

            // Display correct sprite
            if (item.Comp.RaisedEquippedPrefix != null)
            {
                _clothingSystem.SetEquippedPrefix(item.Owner, null, clothingComp);
            }

            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, true);

            Dirty(item);

            if (!_timing.ApplyingState)
            {
                eyeProt = EnsureComp<RMCEyeProtectionComponent>(user);
                Dirty(user, eyeProt);
            }

            _actions.SetToggled(item.Comp.Action, true);
        }

        protected void DisableEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid? user)
        {
            if (!TryComp<ClothingComponent>(item.Owner, out var clothingComp) ||
                !TryComp<ItemComponent>(item.Owner, out var itemComp))
                return;

            item.Comp.User = null;
            item.Comp.Toggled = false;

            // Display correct sprite
            if (item.Comp.RaisedEquippedPrefix != null)
            {
                _clothingSystem.SetEquippedPrefix(item.Owner, item.Comp.RaisedEquippedPrefix, clothingComp);
            }

            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, false);

            Dirty(item);

            if (TryComp(user, out RMCEyeProtectionComponent? eyeProt))
            {
                RemCompDeferred<RMCEyeProtectionComponent>(user.Value);
            }

            _actions.SetToggled(item.Comp.Action, false);
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
