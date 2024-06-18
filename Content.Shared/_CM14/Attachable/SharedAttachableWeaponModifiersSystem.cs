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
        SubscribeLocalEvent<AttachableWeaponRangedModsUnwieldedComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsWieldedComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
    }
    
    
    public void ApplyWeaponModifiers(EntityUid attachableUid, ref GunRefreshModifiersEvent args)
    {
        if(!_entityManager.TryGetComponent<AttachableComponent>(attachableUid, out _))
            return;
        
        if(_entityManager.TryGetComponent<AttachableWeaponRangedModsComponent>(attachableUid, out AttachableWeaponRangedModsComponent? modifiersComponent))
            ApplyWeaponModifiers(modifiersComponent, ref args);
        
        if(_entityManager.TryGetComponent<AttachableWeaponRangedModsUnwieldedComponent>(attachableUid, out AttachableWeaponRangedModsUnwieldedComponent? modifiersUnwieldedComponent))
            ApplyWeaponModifiers(modifiersUnwieldedComponent, ref args);
        
        if(_entityManager.TryGetComponent<AttachableWeaponRangedModsWieldedComponent>(attachableUid, out AttachableWeaponRangedModsWieldedComponent? modifiersWieldedComponent))
            ApplyWeaponModifiers(modifiersWieldedComponent, ref args);
    }
    
    private void ApplyWeaponModifiers(AttachableWeaponRangedModsComponent modifiersComponent, ref GunRefreshModifiersEvent args)
    {
        ApplyModifierSet(modifiersComponent.Modifiers, ref args);
    }
    
    private void ApplyWeaponModifiers(AttachableWeaponRangedModsUnwieldedComponent modifiersComponent, ref GunRefreshModifiersEvent args)
    {
        if(!_entityManager.TryGetComponent<WieldableComponent>(args.Gun.Owner, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded)
            ApplyModifierSet(modifiersComponent.Modifiers, ref args);
    }
    
    private void ApplyWeaponModifiers(AttachableWeaponRangedModsWieldedComponent modifiersComponent, ref GunRefreshModifiersEvent args)
    {
        if(!_entityManager.TryGetComponent<WieldableComponent>(args.Gun.Owner, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded)
            return;
        ApplyModifierSet(modifiersComponent.Modifiers, ref args);
    }
    
    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(attachable.Comp.Modifiers == null || !_entityManager.HasComponent<GunComponent>(args.HolderUid))
            return;
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                ApplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
            
            case AttachableAlteredType.Detached:
                UnapplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
        }
    }
    
    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsUnwieldedComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(attachable.Comp.Modifiers == null || !_entityManager.HasComponent<GunComponent>(args.HolderUid))
            return;
        
        _entityManager.TryGetComponent<WieldableComponent>(args.HolderUid, out WieldableComponent? wieldableComponent);
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                if(wieldableComponent != null && wieldableComponent.Wielded)
                    break;
                ApplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
            
            case AttachableAlteredType.Detached:
                if(wieldableComponent != null && wieldableComponent.Wielded)
                    break;
                UnapplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
            
            case AttachableAlteredType.Wielded:
                UnapplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
            
            case AttachableAlteredType.Unwielded:
                ApplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
        }
    }
    
    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsWieldedComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(attachable.Comp.Modifiers == null || !_entityManager.HasComponent<GunComponent>(args.HolderUid))
            return;
        
        if(!_entityManager.TryGetComponent<WieldableComponent>(args.HolderUid, out WieldableComponent? wieldableComponent))
            return;
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                if(!wieldableComponent.Wielded)
                    break;
                ApplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
            
            case AttachableAlteredType.Detached:
                if(!wieldableComponent.Wielded)
                    break;
                UnapplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
            
            case AttachableAlteredType.Wielded:
                ApplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
            
            case AttachableAlteredType.Unwielded:
                UnapplyModifierSet(attachable.Comp.Modifiers, args.HolderUid);
                break;
        }
    }
    
    private void ApplyModifierSet(AttachableWeaponRangedModifierSet modifierSet, ref GunRefreshModifiersEvent args)
    {
        args.ShotsPerBurst += modifierSet.FlatShotsPerBurst;
        
        args.CameraRecoilScalar *= modifierSet.MultiplierCameraRecoilScalar;
        args.AngleIncrease = new Angle(Math.Max(args.MinAngle.Theta * modifierSet.MultiplierAngleIncrease, 0));
        args.AngleDecay = new Angle(Math.Max(args.MinAngle.Theta * modifierSet.MultiplierAngleDecay, 0.1));
        args.MinAngle = new Angle(Math.Max(args.MinAngle.Theta * modifierSet.MultiplierMaxAngle, 0));
        args.MaxAngle = new Angle(Math.Max(args.MaxAngle.Theta * modifierSet.MultiplierMinAngle, args.MinAngle));
        args.FireRate *= modifierSet.MultiplierFireRate;
        args.ProjectileSpeed *= modifierSet.MultiplierProjectileSpeed;
    }
    
    private void ApplyModifierSet(AttachableWeaponRangedModifierSet modifierSet, EntityUid gunUid)
    {
        _entityManager.EnsureComponent<GunDamageModifierComponent>(gunUid, out GunDamageModifierComponent damageModifierComponent);
        _cmGunSystem.SetGunDamageModifier(damageModifierComponent, damageModifierComponent.Multiplier * modifierSet.MultiplierDamage);
    }
    
    private void UnapplyModifierSet(AttachableWeaponRangedModifierSet modifierSet, EntityUid gunUid)
    {
        _entityManager.EnsureComponent<GunDamageModifierComponent>(gunUid, out GunDamageModifierComponent damageModifierComponent);
        _cmGunSystem.SetGunDamageModifier(damageModifierComponent, damageModifierComponent.Multiplier / modifierSet.MultiplierDamage);
    }
}