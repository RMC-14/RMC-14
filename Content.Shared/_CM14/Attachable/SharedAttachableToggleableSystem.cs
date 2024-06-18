using Content.Shared._CM14.Weapons.Ranged;
using Content.Shared.DoAfter;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Maths;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared._CM14.Attachable;

public sealed class SharedAttachableToggleableSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedAttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    
    
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleStartedEvent>(OnAttachableToggleStarted);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleDoAfterEvent>(OnAttachableToggleDoAfter);
    }
    
    
    private void OnAttachableAltered(Entity<AttachableToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.Detached:
                if(attachable.Comp.SupercedeHolder && 
                    _entityManager.TryGetComponent<AttachableHolderComponent>(args.HolderUid, out AttachableHolderComponent? holderComponent) &&
                    holderComponent.SupercedingAttachable == attachable.Owner)
                    _attachableHolderSystem.SetSupercedingAttachable((args.HolderUid, holderComponent), null);
                attachable.Comp.Active = false;
                break;
            default:
                break;
        }
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
        
        Logger.Log(LogLevel.Debug, $"DoAfter complete.");
        FinishToggle(attachable, (args.Args.Used.Value, holderComponent), args.SlotID);
        args.Handled = true;
    }
    
    private void FinishToggle(Entity<AttachableToggleableComponent> attachable, Entity<AttachableHolderComponent> holder, string slotID)
    {
        Logger.Log(LogLevel.Debug, $"Toggled.");
        attachable.Comp.Active = !attachable.Comp.Active;
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
        
        RaiseLocalEvent(attachable.Owner, new AttachableAlteredEvent(holder.Owner, attachable.Comp.Active ? AttachableAlteredType.Activated : AttachableAlteredType.Deactivated));
    }
}
