using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.StatusEffect;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Content.Shared.Item.ItemToggle.Components;
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

            args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
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
            ToggleEyeProtectionItem(ent, args.Equipee);
        }

        private void OnEyeProtectionItemGotUnequipped(Entity<RMCEyeProtectionItemComponent> ent, ref GotUnequippedEvent args)
        {
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

        public void Toggle(Entity<RMCEyeProtectionComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp))
                return;

            ent.Comp.Enabled = !ent.Comp.Enabled;

            Dirty(ent);
            UpdateAlert((ent, ent.Comp));
        }

        private void UpdateAlert(Entity<RMCEyeProtectionComponent> ent)
        {
            /*
            if (ent.Comp.Alert is { } alert)
            {
                var level = MathF.Max((int) NightVisionState.Off, (int) ent.Comp.State);
                var max = _alerts.GetMaxSeverity(alert);
                var severity = max - ContentHelpers.RoundToLevels(level, (int) NightVisionState.Full, max + 1);
                _alerts.ShowAlert(ent, alert, (short) severity);
            }
*/
            EyeProtectionChanged(ent);
        }

        private void ToggleEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid user)
        {
            if (item.Comp.Toggled == true && item.Comp.Toggleable)
            {
                DisableEyeProtectionItem(item, item.Comp.User);
                return;
            }

            EnableEyeProtectionItem(item, user);
        }

        private void EnableEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid user)
        {
            DisableEyeProtectionItem(item, item.Comp.User);

            item.Comp.User = user;
            item.Comp.Toggled = true;
            Dirty(item);

            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, true);
            //_entManager.AddComponent<RMCEyeProtectionComponent>(user);

            //_statusEffectsSystem.TryAddStatusEffect(item.Comp.User, EyeProtectionStatusEffect,
            //    TimeSpan.FromSeconds(3600), false, EyeProtectionStatusEffect);


            if (!_timing.ApplyingState)
            {
                var eyeProt = EnsureComp<RMCEyeProtectionComponent>(user);
                eyeProt.Enabled = true;
                Dirty(user, eyeProt);
            }


            _actions.SetToggled(item.Comp.Action, true);
        }

        protected virtual void EyeProtectionChanged(Entity<RMCEyeProtectionComponent> ent)
        {
        }

        protected virtual void EyeProtectionRemoved(Entity<RMCEyeProtectionComponent> ent)
        {
        }

        protected void DisableEyeProtectionItem(Entity<RMCEyeProtectionItemComponent> item, EntityUid? user)
        {
            _actions.SetToggled(item.Comp.Action, false);


            item.Comp.Toggled = false;
            Dirty(item);

            _appearance.SetData(item, RMCEyeProtectionItemVisuals.Active, false);

            //if (_entManager.HasComponent<RMCEyeProtectionComponent>(item.Comp.User))
            //    _entManager.RemoveComponent<RMCEyeProtectionComponent>(item.Comp.User);

            //_statusEffectsSystem.TryRemoveStatusEffect(user, EyeProtectionStatusEffect);

            item.Comp.User = null;

            if (TryComp(user, out RMCEyeProtectionComponent? eyeProt))
            {
                RemCompDeferred<RMCEyeProtectionComponent>(user.Value);
            }

        }
    }
}
