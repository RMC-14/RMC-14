using Content.Shared._CM14.Attachable.Components;
using Content.Shared._CM14.Attachable.Events;
using Content.Shared._CM14.Weapons.Common;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._CM14.Attachable;

public sealed class AttachableToggleableSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainerSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableWeaponRangedModsSystem) });
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleActionEvent>(OnAttachableToggleAction);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleDoAfterEvent>(
            OnAttachableToggleDoAfter);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleStartedEvent>(OnAttachableToggleStarted);
        SubscribeLocalEvent<AttachableToggleableComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<AttachableToggleableComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<AttachableToggleableComponent, UniqueActionEvent>(OnUniqueAction);
        SubscribeLocalEvent<AttachableToggleableComponent, GrantAttachableActionsEvent>(OnAddAttachableActions);
        SubscribeLocalEvent<AttachableToggleableComponent, RemoveAttachableActionsEvent>(OnRemoveAttachableActions);

        SubscribeLocalEvent<AttachableToggleableSimpleActivateComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableWeaponRangedModsSystem) });
    }

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
                break;

            case AttachableAlteredType.Attached:
                attachable.Comp.Attached = true;
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

    private void OnUniqueAction(Entity<AttachableToggleableComponent> attachable, ref UniqueActionEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }

    private void OnAttachableToggleStarted(Entity<AttachableToggleableComponent> attachable,
        ref AttachableToggleStartedEvent args)
    {
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            args.User,
            attachable.Comp.DoAfter,
            new AttachableToggleDoAfterEvent(args.SlotId),
            attachable,
            target: attachable.Owner,
            used: args.Holder)
        {
            NeedHand = attachable.Comp.NeedHand,
            BreakOnMove = attachable.Comp.BreakOnMove
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
    }

    private void FinishToggle(Entity<AttachableToggleableComponent> attachable,
        Entity<AttachableHolderComponent> holder,
        string slotId,
        EntityUid? userUid)
    {
        attachable.Comp.Active = !attachable.Comp.Active;

        var mode = attachable.Comp.Active ? AttachableAlteredType.Activated : AttachableAlteredType.Deactivated;
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

    private void OnAddAttachableActions(Entity<AttachableToggleableComponent> ent, ref GrantAttachableActionsEvent args)
    {
        var exists = ent.Comp.Action != null;
        _actionContainerSystem.EnsureAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);

        if (ent.Comp.Action is not { } actionId)
            return;

        _actionsSystem.GrantContainedAction(args.User, ent.Owner, actionId);

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
    }

    private void OnRemoveAttachableActions(Entity<AttachableToggleableComponent> ent, ref RemoveAttachableActionsEvent args)
    {
        if (ent.Comp.Action is not { } action)
            return;

        _actionsSystem.RemoveProvidedAction(args.User, ent, action);
    }

    private void OnAttachableToggleAction(Entity<AttachableToggleableComponent> attachable,
        ref AttachableToggleActionEvent args)
    {
        args.Handled = true;

        if (!attachable.Comp.Attached)
            return;

        if (!TryComp(attachable.Owner, out TransformComponent? transformComponent) ||
            !transformComponent.ParentUid.Valid ||
            !TryComp(transformComponent.ParentUid, out AttachableHolderComponent? holderComponent) ||
            !_attachableHolderSystem.TryGetSlotId(transformComponent.ParentUid, attachable.Owner, out var slotId))
        {
            return;
        }

        var ev = new AttachableToggleStartedEvent((transformComponent.ParentUid, holderComponent),
            args.Performer,
            slotId);
        RaiseLocalEvent(attachable.Owner, ref ev);
    }

    private void OnToggleAction(Entity<AttachableToggleableComponent> attachable, ref ToggleActionEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }

    private void OnAttachableAltered(Entity<AttachableToggleableSimpleActivateComponent> attachable,
        ref AttachableAlteredEvent args)
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
}
