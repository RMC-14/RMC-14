using Content.Shared._CM14.Weapons;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;


namespace Content.Shared._CM14.Attachable;

public sealed class SharedAttachableToggleableSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainerSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableAlteredEvent>(OnAttachableAltered, after: new[] { typeof(SharedAttachableWeaponRangedModsSystem) });
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleActionEvent>(OnAttachableToggleAction);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleDoAfterEvent>(OnAttachableToggleDoAfter);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleStartedEvent>(OnAttachableToggleStarted);
        SubscribeLocalEvent<AttachableToggleableComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<AttachableToggleableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AttachableToggleableComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<AttachableToggleableComponent, UniqueActionEvent>(OnUniqueAction);
    }
    
    
    private void OnMapInit(Entity<AttachableToggleableComponent> attachable, ref MapInitEvent args)
    {
        if(!_actionContainerSystem.EnsureAction(attachable.Owner, ref attachable.Comp.AttachableToggleActionEntity, out BaseActionComponent? actionComponent, attachable.Comp.AttachableToggleAction))
            return;
        
        actionComponent.Icon = attachable.Comp.Icon;
        actionComponent.IconOn = attachable.Comp.IconActive;
        actionComponent.Enabled = attachable.Comp.Attached;
        
        _metaDataSystem.SetEntityName(attachable.Comp.AttachableToggleActionEntity.Value, attachable.Comp.AttachableToggleActionName);
        _metaDataSystem.SetEntityDescription(attachable.Comp.AttachableToggleActionEntity.Value, attachable.Comp.AttachableToggleActionDesc);
        
        Dirty(attachable);
    }
    
    private void OnAttachableAltered(Entity<AttachableToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.Detached:
                if(attachable.Comp.SupercedeHolder && 
                    _entityManager.TryGetComponent<AttachableHolderComponent>(args.HolderUid, out AttachableHolderComponent? holderComponent) &&
                    holderComponent.SupercedingAttachable == attachable.Owner)
                {
                    _attachableHolderSystem.SetSupercedingAttachable((args.HolderUid, holderComponent), null);
                }
                
                if(attachable.Comp.Active)
                    RaiseLocalEvent(attachable.Owner, new AttachableAlteredEvent(args.HolderUid, AttachableAlteredType.DetachedDeactivated, args.UserUid));
                
                attachable.Comp.Attached = false;
                attachable.Comp.Active = false;
                break;
            
            case AttachableAlteredType.Attached:
                attachable.Comp.Attached = true;
                break;
            
            default:
                break;
        }
        
        if(attachable.Comp.AttachableToggleActionEntity == null ||
            !_entityManager.TryGetComponent<InstantActionComponent>(attachable.Comp.AttachableToggleActionEntity, out InstantActionComponent? actionComponent))
        {
            return;
        }
        
        
        _actionsSystem.SetToggled(attachable.Comp.AttachableToggleActionEntity, attachable.Comp.Active);
        actionComponent.Enabled = attachable.Comp.Attached;
    }
    
    private void OnActivateInWorld(Entity<AttachableToggleableComponent> attachable, ref ActivateInWorldEvent args)
    {
        if(attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }
    
    private void OnAttemptShoot(Entity<AttachableToggleableComponent> attachable, ref AttemptShootEvent args)
    {
        if(attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Cancelled = true;
    }
    
    private void OnUniqueAction(Entity<AttachableToggleableComponent> attachable, ref UniqueActionEvent args)
    {
        if(attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }
    
    private void OnAttachableToggleStarted(Entity<AttachableToggleableComponent> attachable, ref AttachableToggleStartedEvent args)
    {
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            _entityManager,
            args.UserUid,
            attachable.Comp.DoAfter,
            new AttachableToggleDoAfterEvent(args.SlotID),
            attachable,
            target: attachable.Owner,
            used: args.Holder)
        {
            NeedHand = attachable.Comp.NeedHand,
            BreakOnMove = attachable.Comp.BreakOnMove
        });
        
        Dirty(attachable);
    }
    
    private void OnAttachableToggleDoAfter(Entity<AttachableToggleableComponent> attachable, ref AttachableToggleDoAfterEvent args)
    {
        if(args.Cancelled || args.Handled || args.Args.Target == null)
            return;
        
        if(!_entityManager.HasComponent<AttachableToggleableComponent>(args.Args.Target))
            return;
        
        if(!_entityManager.TryGetComponent<AttachableHolderComponent>(args.Args.Used, out AttachableHolderComponent? holderComponent))
            return;
        
        FinishToggle(attachable, (args.Args.Used.Value, holderComponent), args.SlotID, args.Args.User);
        _audioSystem.PlayPredicted(attachable.Comp.Active ? attachable.Comp.ActivateSound : attachable.Comp.DeactivateSound, attachable, args.User);
        args.Handled = true;
    }
    
    private void FinishToggle(Entity<AttachableToggleableComponent> attachable, Entity<AttachableHolderComponent> holder, string slotID, EntityUid? userUid)
    {
        attachable.Comp.Active = !attachable.Comp.Active;
        RaiseLocalEvent(attachable.Owner, new AttachableAlteredEvent(holder.Owner, attachable.Comp.Active ? AttachableAlteredType.Activated : AttachableAlteredType.Deactivated, userUid));
        RaiseLocalEvent(holder.Owner,
            new AttachableHolderAttachablesAlteredEvent(attachable.Owner, slotID, attachable.Comp.Active ? AttachableAlteredType.Activated : AttachableAlteredType.Deactivated));
        
        if(!attachable.Comp.Active)
        {
            if(attachable.Comp.SupercedeHolder && holder.Comp.SupercedingAttachable == attachable.Owner)
                _attachableHolderSystem.SetSupercedingAttachable(holder, null);
            return;
        }
        
        if(attachable.Comp.SupercedeHolder)
        {
            if(holder.Comp.SupercedingAttachable != null
                && _entityManager.TryGetComponent<AttachableToggleableComponent>(holder.Comp.SupercedingAttachable, out AttachableToggleableComponent? toggleableComponent))
            {
                toggleableComponent.Active = false;
                RaiseLocalEvent(holder.Comp.SupercedingAttachable.Value, new AttachableAlteredEvent(holder.Owner, AttachableAlteredType.Deactivated));
                if(_attachableHolderSystem.TryGetSlotID(holder.Owner, attachable.Owner, out string? deactivatedSlot))
                    RaiseLocalEvent(holder.Owner, new AttachableHolderAttachablesAlteredEvent(holder.Comp.SupercedingAttachable.Value, deactivatedSlot, AttachableAlteredType.Deactivated));
            }
            _attachableHolderSystem.SetSupercedingAttachable(holder, attachable.Owner);
        }
    }
    
    public void GrantAction(Entity<AttachableToggleableComponent> attachable, EntityUid performer)
    {
        if(attachable.Comp.AttachableToggleActionEntity == null)
            return;
        
        EntityUid[] entityUids = { attachable.Comp.AttachableToggleActionEntity.Value };
        
        _actionsSystem.GrantActions(performer, entityUids, attachable.Owner);
    }
    
    public void RevokeAction(Entity<AttachableToggleableComponent> attachable, EntityUid performer)
    {
        if(attachable.Comp.AttachableToggleActionEntity == null)
            return;
        
        _actionsSystem.RemoveProvidedAction(performer, attachable.Owner, attachable.Comp.AttachableToggleActionEntity.Value);
    }
    
    private void OnAttachableToggleAction(Entity<AttachableToggleableComponent> attachable, ref AttachableToggleActionEvent args)
    {
        args.Handled = true;
        
        if(!attachable.Comp.Attached)
            return;
        
        if(!_entityManager.TryGetComponent<TransformComponent>(attachable.Owner, out TransformComponent? transformComponent) ||
            transformComponent.ParentUid == EntityUid.Invalid ||
            !_entityManager.TryGetComponent<AttachableHolderComponent>(transformComponent.ParentUid, out AttachableHolderComponent? holderComponent) ||
            !_attachableHolderSystem.TryGetSlotID(transformComponent.ParentUid, attachable.Owner, out string? slotID))
        {
            return;
        }
        
        RaiseLocalEvent(attachable.Owner, new AttachableToggleStartedEvent((transformComponent.ParentUid, holderComponent), args.Performer, slotID));
    }
    
    private void OnToggleAction(Entity<AttachableToggleableComponent> attachable, ref ToggleActionEvent args)
    {
        if(attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }
}
