using Content.Shared._CM14.Weapons;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;


namespace Content.Shared._CM14.Attachable;

public sealed class SharedAttachableToggleableSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    
    
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableAlteredEvent>(OnAttachableAltered, after: new[]
            {
                typeof(SharedAttachableWeaponRangedModsSystem)
            });
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleStartedEvent>(OnAttachableToggleStarted);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleDoAfterEvent>(OnAttachableToggleDoAfter);
        SubscribeLocalEvent<AttachableToggleableComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<AttachableToggleableComponent, AttemptShootEvent>(OnAttemptShoot);
        //SubscribeLocalEvent<AttachableToggleableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AttachableToggleableComponent, UniqueActionEvent>(OnUniqueAction);
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
    
    private void OnInteractUsing(Entity<AttachableToggleableComponent> attachable, ref InteractUsingEvent args)
    {
        if(attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
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
}
