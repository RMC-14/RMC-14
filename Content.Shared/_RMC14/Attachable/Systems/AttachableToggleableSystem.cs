using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableToggleableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainerSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableModifiersSystem) });
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleActionEvent>(OnAttachableToggleAction);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleDoAfterEvent>(OnAttachableToggleDoAfter);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleStartedEvent>(OnAttachableToggleStarted);
        SubscribeLocalEvent<AttachableToggleableComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<AttachableToggleableComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<AttachableToggleableComponent, ToggleActionEvent>(OnToggleAction,
            before: new[] { typeof(SharedHandheldLightSystem) });
        //SubscribeLocalEvent<AttachableToggleableComponent, UniqueActionEvent>(OnUniqueAction);
        SubscribeLocalEvent<AttachableToggleableComponent, GrantAttachableActionsEvent>(OnGrantAttachableActions);
        SubscribeLocalEvent<AttachableToggleableComponent, RemoveAttachableActionsEvent>(OnRemoveAttachableActions);
        SubscribeLocalEvent<AttachableToggleableComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<AttachableToggleableComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<AttachableToggleableComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);

        SubscribeLocalEvent<AttachableMovementLockedComponent, MoveInputEvent>(OnAttachableMovementLockedMoveInput);

        SubscribeLocalEvent<AttachableToggleableSimpleActivateComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableModifiersSystem) });

        SubscribeLocalEvent<AttachableToggleablePreventShootComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableModifiersSystem) });
        
        SubscribeLocalEvent<AttachableGunPreventShootComponent, AttemptShootEvent>(OnAttemptShoot);
    }

#region AttachableAlteredEvent handling
    private void OnAttachableAltered(Entity<AttachableToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Detached:
                if (attachable.Comp.SupercedeHolder &&
                    TryComp(args.Holder, out AttachableHolderComponent? holderComponent) &&
                    holderComponent.SupercedingAttachable == attachable.Owner)
                {
                    _attachableHolderSystem.SetSupercedingAttachable((args.Holder, holderComponent), null);
                }

                if (attachable.Comp.Active)
                {
                    var ev = args with { Alteration = AttachableAlteredType.DetachedDeactivated };
                    RaiseLocalEvent(attachable.Owner, ref ev);
                }

                attachable.Comp.Attached = false;
                attachable.Comp.Active = false;
                Dirty(attachable);
                break;

            case AttachableAlteredType.Attached:
                attachable.Comp.Attached = true;
                break;

            case AttachableAlteredType.Unwielded:
                if (!attachable.Comp.WieldedOnly || !attachable.Comp.Active)
                    break;

                Toggle(attachable, args.User, attachable.Comp.DoInterrupt);
                break;
        }

        if (attachable.Comp.Action == null ||
            !TryComp(attachable.Comp.Action, out InstantActionComponent? actionComponent))
        {
            return;
        }

        _actionsSystem.SetToggled(attachable.Comp.Action, attachable.Comp.Active);
        actionComponent.Enabled = attachable.Comp.Attached;
    }

    private void OnAttachableAltered(Entity<AttachableToggleableSimpleActivateComponent> attachable, ref AttachableAlteredEvent args)
    {
        if (args.User == null)
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Activated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;

            case AttachableAlteredType.Deactivated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;

            case AttachableAlteredType.DetachedDeactivated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;
        }
    }

    private void OnAttachableAltered(Entity<AttachableToggleablePreventShootComponent> attachable, ref AttachableAlteredEvent args)
    {
        if (!TryComp(attachable.Owner, out AttachableToggleableComponent? toggleableComponent))
            return;

        EnsureComp(args.Holder, out AttachableGunPreventShootComponent preventShootComponent);

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                preventShootComponent.Message = attachable.Comp.Message;
                preventShootComponent.PreventShoot = attachable.Comp.ShootWhenActive && !toggleableComponent.Active || !attachable.Comp.ShootWhenActive && toggleableComponent.Active;
                break;

            case AttachableAlteredType.Detached:
                preventShootComponent.Message = "";
                break;

            case AttachableAlteredType.Activated:
                preventShootComponent.PreventShoot = !attachable.Comp.ShootWhenActive;
                break;

            case AttachableAlteredType.Deactivated:
                preventShootComponent.PreventShoot = attachable.Comp.ShootWhenActive;
                break;

            case AttachableAlteredType.DetachedDeactivated:
                preventShootComponent.PreventShoot = false;
                break;
        }
        
        Dirty(args.Holder, preventShootComponent);
    }
#endregion

    private void OnGotEquippedHand(Entity<AttachableToggleableComponent> attachable, ref GotEquippedHandEvent args)
    {
        if (!attachable.Comp.Attached || args.Equipped == attachable.Owner)
            return;

        args.Handled = true;

        GrantAttachableActions(attachable, args.User);
    }

#region Lockouts and interrupts
    private void OnActivateInWorld(Entity<AttachableToggleableComponent> attachable, ref ActivateInWorldEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }

    private void OnAttemptShoot(Entity<AttachableToggleableComponent> attachable, ref AttemptShootEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Cancelled = true;
    }

    private void OnGunShot(Entity<AttachableToggleableComponent> attachable, ref GunShotEvent args)
    {
        CheckUserBreakOnRotate(args.User);
    }

    private void OnAttemptShoot(Entity<AttachableGunPreventShootComponent> gun, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !gun.Comp.PreventShoot)
            return;

        args.Cancelled = true;

        _popupSystem.PopupClient(gun.Comp.Message, args.User, args.User);
    }

/*    private void OnUniqueAction(Entity<AttachableToggleableComponent> attachable, ref UniqueActionEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }*/

    private void OnHandDeselected(Entity<AttachableToggleableComponent> attachable, ref HandDeselectedEvent args)
    {
        if (!attachable.Comp.Attached)
            return;
        
        args.Handled = true;
        
        if (!attachable.Comp.NeedHand || !attachable.Comp.Active)
            return;

        Toggle(attachable, args.User, attachable.Comp.DoInterrupt);
    }

    private void OnGotUnequippedHand(Entity<AttachableToggleableComponent> attachable, ref GotUnequippedHandEvent args)
    {
        if (!attachable.Comp.Attached || args.Unequipped == attachable.Owner)
            return;
        
        args.Handled = true;
        
        RemoveAttachableActions(attachable, args.User);
        
        if (!attachable.Comp.NeedHand || !attachable.Comp.Active)
            return;

        Toggle(attachable, args.User, attachable.Comp.DoInterrupt);
    }
    
    private void OnAttachableMovementLockedMoveInput(Entity<AttachableMovementLockedComponent> user, ref MoveInputEvent args)
    {
        foreach (EntityUid attachableUid in user.Comp.AttachableList)
        {
            if (!TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent) ||
                !toggleableComponent.Active ||
                !toggleableComponent.BreakOnMove)
            {
                continue;
            }
            
            Toggle((attachableUid, toggleableComponent), user.Owner, toggleableComponent.DoInterrupt);
        }

        RemCompDeferred<AttachableMovementLockedComponent>(user);
    }

    private void CheckUserBreakOnRotate(Entity<AttachableDirectionLockedComponent?> user)
    {
        if (user.Comp == null)
        {
            if (!TryComp(user.Owner, out AttachableDirectionLockedComponent? lockedComponent))
                return;

            user.Comp = lockedComponent;
        }

        if (Transform(user.Owner).LocalRotation.GetCardinalDir() == user.Comp.LockedDirection)
            return;

        foreach (EntityUid attachableUid in user.Comp.AttachableList)
        {
            if (!TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent) ||
                !toggleableComponent.Active ||
                !toggleableComponent.BreakOnRotate)
            {
                continue;
            }

            Toggle((attachableUid, toggleableComponent), user.Owner, toggleableComponent.DoInterrupt);
        }

        RemCompDeferred<AttachableDirectionLockedComponent>(user);
    }
#endregion

#region Toggling
    private void OnAttachableToggleStarted(Entity<AttachableToggleableComponent> attachable, ref AttachableToggleStartedEvent args)
    {
        if (!attachable.Comp.Active && attachable.Comp.WieldedOnly && (!TryComp(args.Holder.Owner, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded))
        {
            _popupSystem.PopupClient(
                Loc.GetString("rmc-attachable-activation-fail-not-wielded", ("holder", args.Holder), ("attachable", attachable)),
                args.User,
                args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            args.User,
            attachable.Comp.DoAfter,
            new AttachableToggleDoAfterEvent(args.SlotId),
            attachable,
            target: attachable.Owner,
            used: args.Holder)
        {
            NeedHand = attachable.Comp.DoAfterNeedHand,
            BreakOnMove = attachable.Comp.DoAfterBreakOnMove
        });

        Dirty(attachable);
    }

    private void OnAttachableToggleDoAfter(Entity<AttachableToggleableComponent> attachable,
        ref AttachableToggleDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } target || args.Used is not { } used)
            return;

        if (!HasComp<AttachableToggleableComponent>(target))
            return;

        if (!TryComp(args.Used, out AttachableHolderComponent? holderComponent))
            return;

        FinishToggle(attachable, (used, holderComponent), args.SlotId, args.User);
        _audioSystem.PlayPredicted(
            attachable.Comp.Active ? attachable.Comp.ActivateSound : attachable.Comp.DeactivateSound,
            attachable,
            args.User);
        args.Handled = true;
        Dirty(attachable);
    }

    private void FinishToggle(
        Entity<AttachableToggleableComponent> attachable,
        Entity<AttachableHolderComponent> holder,
        string slotId,
        EntityUid? userUid,
        bool interrupted = false)
    {
        attachable.Comp.Active = !attachable.Comp.Active;

        var mode = attachable.Comp.Active
            ? AttachableAlteredType.Activated
            : interrupted ? AttachableAlteredType.Interrupted : AttachableAlteredType.Deactivated;
        var ev = new AttachableAlteredEvent(holder.Owner, mode, userUid);
        RaiseLocalEvent(attachable.Owner, ref ev);

        var holderEv = new AttachableHolderAttachablesAlteredEvent(attachable.Owner, slotId, mode);
        RaiseLocalEvent(holder.Owner, ref holderEv);

        if (!attachable.Comp.Active)
        {
            if (attachable.Comp.SupercedeHolder && holder.Comp.SupercedingAttachable == attachable.Owner)
                _attachableHolderSystem.SetSupercedingAttachable(holder, null);
            return;
        }
        
        if (attachable.Comp.BreakOnMove && userUid != null)
        {
            var movementLockedComponent = EnsureComp<AttachableMovementLockedComponent>(userUid.Value);
            movementLockedComponent.AttachableList.Add(attachable.Owner);
        }

        if (attachable.Comp.BreakOnRotate && userUid != null)
        {
            var directionLockedComponent = EnsureComp<AttachableDirectionLockedComponent>(userUid.Value);
            directionLockedComponent.AttachableList.Add(attachable.Owner);

            if (directionLockedComponent.LockedDirection == null)
                directionLockedComponent.LockedDirection = Transform(userUid.Value).LocalRotation.GetCardinalDir();
        }

        if (!attachable.Comp.SupercedeHolder)
            return;

        if (holder.Comp.SupercedingAttachable != null &&
            TryComp(holder.Comp.SupercedingAttachable, out AttachableToggleableComponent? toggleableComponent))
        {
            toggleableComponent.Active = false;
            ev = new AttachableAlteredEvent(holder.Owner, AttachableAlteredType.Deactivated);
            RaiseLocalEvent(holder.Comp.SupercedingAttachable.Value, ref ev);

            if (_attachableHolderSystem.TryGetSlotId(holder.Owner, attachable.Owner, out var deactivatedSlot))
            {
                holderEv = new AttachableHolderAttachablesAlteredEvent(holder.Comp.SupercedingAttachable.Value,
                    deactivatedSlot,
                    AttachableAlteredType.Deactivated);
                RaiseLocalEvent(holder.Owner, ref holderEv);
            }
        }

        _attachableHolderSystem.SetSupercedingAttachable(holder, attachable.Owner);
    }

    private void Toggle(Entity<AttachableToggleableComponent> attachable, EntityUid? user, bool interrupted = false)
    {
        if (!_attachableHolderSystem.TryGetHolder(attachable.Owner, out var holderUid) ||
            !TryComp(holderUid, out AttachableHolderComponent? holderComponent) ||
            !_attachableHolderSystem.TryGetSlotId(holderUid.Value, attachable.Owner, out var slotId))
        {
            return;
        }
        
        FinishToggle(attachable, (holderUid.Value, holderComponent), slotId, user, interrupted);
        Dirty(attachable);
    }
#endregion

#region Actions
    private void OnGrantAttachableActions(Entity<AttachableToggleableComponent> ent, ref GrantAttachableActionsEvent args)
    {
        GrantAttachableActions(ent, args.User);
    }
    
    private void GrantAttachableActions(Entity<AttachableToggleableComponent> ent, EntityUid user, bool doSecondTry = true)
    {
        // This is to prevent ActionContainerSystem from shitting itself if the attachment has actions other than its attachment toggle.
        if (!TryComp(ent.Owner, out ActionsContainerComponent? actionsContainerComponent) || actionsContainerComponent.Container == null)
        {
            EnsureComp<ActionsContainerComponent>(ent.Owner);

            if (doSecondTry)
                GrantAttachableActions(ent, user, false);

            return;
        }

        var exists = ent.Comp.Action != null;
        _actionContainerSystem.EnsureAction(ent, ref ent.Comp.Action, ent.Comp.ActionId, actionsContainerComponent);

        if (ent.Comp.Action is not { } actionId)
            return;

        _actionsSystem.GrantContainedAction(user, ent.Owner, actionId);

        if (exists)
            return;

        _metaDataSystem.SetEntityName(actionId, ent.Comp.ActionName);
        _metaDataSystem.SetEntityDescription(actionId, ent.Comp.ActionDesc);

        if (_actionsSystem.TryGetActionData(actionId, out var action))
        {
            action.Icon = ent.Comp.Icon;
            action.IconOn = ent.Comp.IconActive;
            action.Enabled = ent.Comp.Attached;
            Dirty(actionId, action);
        }

        Dirty(ent);
    }

    private void OnRemoveAttachableActions(Entity<AttachableToggleableComponent> ent, ref RemoveAttachableActionsEvent args)
    {
        RemoveAttachableActions(ent, args.User);
    }
    
    private void RemoveAttachableActions(Entity<AttachableToggleableComponent> ent, EntityUid user)
    {
        if (ent.Comp.Action is not { } action)
            return;
        
        if (!TryComp(action, out InstantActionComponent? actionComponent) || actionComponent.AttachedEntity != user)
            return;
        
        _actionsSystem.RemoveProvidedAction(user, ent, action);
    }

    private void OnAttachableToggleAction(Entity<AttachableToggleableComponent> attachable,
        ref AttachableToggleActionEvent args)
    {
        args.Handled = true;

        if (!attachable.Comp.Attached)
            return;

        if (!_attachableHolderSystem.TryGetHolder(attachable.Owner, out var holderUid) ||
            !TryComp(holderUid, out AttachableHolderComponent? holderComponent) ||
            !_attachableHolderSystem.TryGetSlotId(holderUid.Value, attachable.Owner, out var slotId))
        {
            return;
        }

        var ev = new AttachableToggleStartedEvent((holderUid.Value, holderComponent),
            args.Performer,
            slotId);
        RaiseLocalEvent(attachable.Owner, ref ev);
    }

    private void OnToggleAction(Entity<AttachableToggleableComponent> attachable, ref ToggleActionEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }
#endregion
}
