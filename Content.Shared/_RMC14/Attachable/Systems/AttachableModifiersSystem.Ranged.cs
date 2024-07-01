using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeRanged()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, GunRefreshModifiersEvent>(OnRangedModsRefreshModifiers);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableAlteredEvent>(OnRangedModsAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, GetGunDamageModifierEvent>(OnRangedModsGetGunDamage);
    }

    private void OnRangedModsRefreshModifiers(Entity<AttachableWeaponRangedModsComponent> attachable, ref GunRefreshModifiersEvent args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args);
        }
    }

    private void OnRangedModsAltered(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch(args.Alteration)
        {
            case AttachableAlteredType.AppearanceChanged:
                break;

            case AttachableAlteredType.DetachedDeactivated:
                break;

            default:
                _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);
                break;
        }
    }

    private void OnRangedModsGetGunDamage(Entity<AttachableWeaponRangedModsComponent> attachable, ref GetGunDamageModifierEvent args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args);
        }
    }

    private void ApplyModifierSet(
        Entity<AttachableWeaponRangedModsComponent> attachable,
        AttachableWeaponRangedModifierSet modSet,
        ref GunRefreshModifiersEvent args)
    {
        if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
            return;

        args.ShotsPerBurst += modSet.ShotsPerBurstFlat;
        args.CameraRecoilScalar = Math.Max(args.CameraRecoilScalar + modSet.RecoilFlat, 0);
        args.MinAngle = Angle.FromDegrees(Math.Max(args.MinAngle.Degrees + modSet.ScatterFlat, 0.0));
        args.MaxAngle = Angle.FromDegrees(Math.Max(args.MaxAngle.Degrees + modSet.ScatterFlat, args.MinAngle));
        args.ProjectileSpeed += modSet.ProjectileSpeedFlat;

        // Fire delay doesn't work quite like SS14 fire rate, so we're having to do maths:
        // Fire rate is shots per second. Fire delay is the interval between shots. They are inversely proportionate to each other.
        // First we divide 1 second by the fire rate to get our current fire delay, then we add the delay modifier, then we divide 1 by the result again to get the modified fire rate.
        args.FireRate = 1.0f / (1.0f / args.FireRate + modSet.FireDelayFlat);
    }

    private void ApplyModifierSet(
        Entity<AttachableWeaponRangedModsComponent> attachable,
        AttachableWeaponRangedModifierSet modSet,
        ref GetGunDamageModifierEvent args)
    {
        if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
            return;

        args.Multiplier += modSet.DamageAddMult;
    }
}
