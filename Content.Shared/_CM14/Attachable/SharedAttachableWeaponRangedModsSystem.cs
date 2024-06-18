using Content.Shared._CM14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Maths;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared._CM14.Attachable;

public sealed class SharedAttachableWeaponRangedModsSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;
    
    
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsToggleableComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
    }
    
    
    public void ApplyWeaponModifiers(EntityUid attachableUid, ref GunRefreshModifiersEvent args)
    {
        if(!_entityManager.TryGetComponent<AttachableComponent>(attachableUid, out _))
            return;
        
        if(_entityManager.TryGetComponent<AttachableWeaponRangedModsComponent>(attachableUid, out AttachableWeaponRangedModsComponent? modifiersComponent))
            ApplyWeaponModifiers(modifiersComponent, attachableUid, ref args);
        
        if(_entityManager.TryGetComponent<AttachableWeaponRangedModsToggleableComponent>(attachableUid, out AttachableWeaponRangedModsToggleableComponent? modifiersToggleableComponent))
            ApplyWeaponModifiers(modifiersToggleableComponent, attachableUid, ref args);
    }
    
    private void ApplyWeaponModifiers(AttachableWeaponRangedModsComponent modifiersComponent, EntityUid attachableUid, ref GunRefreshModifiersEvent args)
    {
        WieldedUnwieldedApplyModifiers(modifiersComponent.ModifiersUnwielded, modifiersComponent.ModifiersWielded, args.Gun.Owner, ref args);
    }
    
    private void ApplyWeaponModifiers(AttachableWeaponRangedModsToggleableComponent modifiersComponent, EntityUid attachableUid, ref GunRefreshModifiersEvent args)
    {
        if(!_entityManager.TryGetComponent<AttachableToggleableComponent>(attachableUid, out AttachableToggleableComponent? toggleableComponent))
            return;
        
        if(toggleableComponent.Active)
        {
            WieldedUnwieldedApplyModifiers(modifiersComponent.ModifiersActiveUnwielded, modifiersComponent.ModifiersActiveWielded, args.Gun.Owner, ref args);
            return;
        }
        WieldedUnwieldedApplyModifiers(modifiersComponent.ModifiersInactiveUnwielded, modifiersComponent.ModifiersInactiveWielded, args.Gun.Owner, ref args);
    }
    
    private void WieldedUnwieldedApplyModifiers(
        AttachableWeaponRangedModifierSet modifiers,
        AttachableWeaponRangedModifierSet wieldedModifiers,
        EntityUid holderUid,
        ref GunRefreshModifiersEvent args)
    {
        if(!_entityManager.TryGetComponent<WieldableComponent>(holderUid, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded)
        {
            ApplyModifierSet(modifiers, ref args);
            return;
        }
        ApplyModifierSet(wieldedModifiers, ref args);
    }
    
    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(!_entityManager.HasComponent<GunComponent>(args.HolderUid))
            return;
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                WieldedUnwieldedChangeModifierSet(attachable.Comp.ModifiersUnwielded, attachable.Comp.ModifiersWielded, args.HolderUid);
                break;
            
            case AttachableAlteredType.Detached:
                WieldedUnwieldedChangeModifierSet(attachable.Comp.ModifiersUnwielded, attachable.Comp.ModifiersWielded, args.HolderUid, apply: false);
                break;
            
            case AttachableAlteredType.Wielded:
                SwapModifierSet(attachable.Comp.ModifiersUnwielded, attachable.Comp.ModifiersWielded, args.HolderUid);
                break;
            
            case AttachableAlteredType.Unwielded:
                SwapModifierSet(attachable.Comp.ModifiersWielded, attachable.Comp.ModifiersUnwielded, args.HolderUid);
                break;
        }
    }
    
    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(!_entityManager.HasComponent<GunComponent>(args.HolderUid))
            return;
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid);
                break;
            
            case AttachableAlteredType.Detached:
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid, apply: false);
                break;
            
            case AttachableAlteredType.Wielded:
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid, swapWielded: true);
                break;
            
            case AttachableAlteredType.Unwielded:
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid, swapWielded: true);
                break;
            
            case AttachableAlteredType.Activated:
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid, apply: false, invertActive: true);
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid);
                break;
            
            case AttachableAlteredType.Deactivated:
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid, apply: false, invertActive: true);
                ActiveInactiveChangeModifierSet(attachable, args.HolderUid);
                break;
        }
    }
    
    private void ActiveInactiveChangeModifierSet(
        Entity<AttachableWeaponRangedModsToggleableComponent> attachable,
        EntityUid holderUid,
        bool apply = true,
        bool invertActive = false,
        bool swapWielded = false)
    {
        if(!_entityManager.TryGetComponent<AttachableToggleableComponent>(attachable.Owner, out AttachableToggleableComponent? toggleableComponent))
            return;
        
        if(toggleableComponent.Active && !invertActive || !toggleableComponent.Active && invertActive)
        {
            WieldedUnwieldedChangeModifierSet(attachable.Comp.ModifiersActiveUnwielded, attachable.Comp.ModifiersActiveWielded, holderUid, apply, swapWielded);
            return;
        }
        
        WieldedUnwieldedChangeModifierSet(attachable.Comp.ModifiersInactiveUnwielded, attachable.Comp.ModifiersInactiveWielded, holderUid, apply, swapWielded);
    }
    
    private void WieldedUnwieldedChangeModifierSet(
        AttachableWeaponRangedModifierSet modifiers,
        AttachableWeaponRangedModifierSet wieldedModifiers,
        EntityUid holderUid,
        bool apply = true,
        bool swap = false)
    {
        if(!_entityManager.TryGetComponent<WieldableComponent>(holderUid, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded)
        {
            if(swap)
            {
                SwapModifierSet(wieldedModifiers, modifiers, holderUid);
                return;
            }
            
            ChangeModifierSet(modifiers, holderUid, apply);
            return;
        }
        
        if(swap)
        {
            SwapModifierSet(modifiers, wieldedModifiers, holderUid);
            return;
        }
        
        ChangeModifierSet(wieldedModifiers, holderUid, apply);
    }
    
    private void ApplyModifierSet(AttachableWeaponRangedModifierSet modifierSet, ref GunRefreshModifiersEvent args)
    {
        args.ShotsPerBurst += modifierSet.FlatShotsPerBurst;
        
        args.CameraRecoilScalar *= modifierSet.MultiplierCameraRecoilScalar;
        args.AngleIncrease = new Angle(Math.Max(args.AngleIncrease.Theta * modifierSet.MultiplierAngleIncrease, 0.0));
        args.AngleDecay = new Angle(Math.Max(args.AngleDecay.Theta * modifierSet.MultiplierAngleDecay, 0.0));
        args.MinAngle = new Angle(Math.Max(args.MinAngle.Theta * modifierSet.MultiplierMinAngle, 0.0));
        args.MaxAngle = new Angle(Math.Max(args.MaxAngle.Theta * modifierSet.MultiplierMaxAngle, args.MinAngle));
        args.FireRate *= modifierSet.MultiplierFireRate;
        args.ProjectileSpeed *= modifierSet.MultiplierProjectileSpeed;
    }
    
    private void SwapModifierSet(AttachableWeaponRangedModifierSet setToRemove, AttachableWeaponRangedModifierSet setToApply, EntityUid gunUid)
    {
        ChangeModifierSet(setToRemove, gunUid, apply: false);
        ChangeModifierSet(setToApply, gunUid);
    }
    
    private void ChangeModifierSet(AttachableWeaponRangedModifierSet modifierSet, EntityUid gunUid, bool apply = true)
    {
        _entityManager.EnsureComponent<GunDamageModifierComponent>(gunUid, out GunDamageModifierComponent damageModifierComponent);
        
        if(apply)
        {
            _cmGunSystem.SetGunDamageModifier(damageModifierComponent, damageModifierComponent.Multiplier * modifierSet.MultiplierDamage);
            return;
        }
        
        _cmGunSystem.SetGunDamageModifier(damageModifierComponent, damageModifierComponent.Multiplier / modifierSet.MultiplierDamage);
    }
}