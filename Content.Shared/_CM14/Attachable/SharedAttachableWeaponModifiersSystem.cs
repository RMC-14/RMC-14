using Content.Shared._CM14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Maths;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared._CM14.Attachable;

public sealed class SharedAttachableWeaponModifiersSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;
    
    
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModifiersComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
    }
    
    
    public void ApplyWeaponModifiers(EntityUid attachableUid, ref GunRefreshModifiersEvent args)
    {
        if(!_entityManager.TryGetComponent<AttachableComponent>(attachableUid, out _) ||
            !_entityManager.TryGetComponent<AttachableWeaponRangedModifiersComponent>(attachableUid, out AttachableWeaponRangedModifiersComponent? modifiersComponent))
            return;
        
        if(modifiersComponent.ModifiersWielded == null || modifiersComponent.ModifiersWielded == null)
            return;
        
        if(!_entityManager.TryGetComponent<WieldableComponent>(args.Gun.Owner, out WieldableComponent? wieldableComponent))
        {
            ApplyModifierSet(modifiersComponent.ModifiersUnwielded, ref args);
            return;
        }
        
        if(wieldableComponent.Wielded)
            ApplyModifierSet(modifiersComponent.ModifiersWielded, ref args);
        else
            ApplyModifierSet(modifiersComponent.ModifiersUnwielded, ref args);
    }
    
    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModifiersComponent> attachable, ref AttachableAlteredEvent args)
    {
        if(attachable.Comp.ModifiersWielded == null || attachable.Comp.ModifiersUnwielded == null)
            return;
        
        if(!_entityManager.TryGetComponent<GunComponent>(args.HolderUid, out _))
            return;
        
        _entityManager.TryGetComponent<WieldableComponent>(args.HolderUid, out WieldableComponent? wieldableComponent);
        
        
        switch(args.Alteration)
        {
            case AttachableAlteredType.Attached:
                if(wieldableComponent != null && wieldableComponent.Wielded)
                    ApplyModifierSet(attachable.Comp.ModifiersWielded, args.HolderUid);
                else
                    ApplyModifierSet(attachable.Comp.ModifiersUnwielded, args.HolderUid);
                return;
            case AttachableAlteredType.Detached:
                if(wieldableComponent != null && wieldableComponent.Wielded)
                    UnapplyModifierSet(attachable.Comp.ModifiersWielded, args.HolderUid);
                else
                    UnapplyModifierSet(attachable.Comp.ModifiersUnwielded, args.HolderUid);
                return;
            case AttachableAlteredType.Wielded:
                UnapplyModifierSet(attachable.Comp.ModifiersUnwielded, args.HolderUid);
                ApplyModifierSet(attachable.Comp.ModifiersWielded, args.HolderUid);
                return;
            case AttachableAlteredType.Unwielded:
                UnapplyModifierSet(attachable.Comp.ModifiersWielded, args.HolderUid);
                ApplyModifierSet(attachable.Comp.ModifiersUnwielded, args.HolderUid);
                return;
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