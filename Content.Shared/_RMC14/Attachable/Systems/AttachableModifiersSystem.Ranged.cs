using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;

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
        if (args.Alteration == AttachableAlteredType.AppearanceChanged || args.Alteration == AttachableAlteredType.DetachedDeactivated)
            return;
        
        _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);
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
        args.CameraRecoilScalar += modSet.RecoilFlat;
        args.AngleIncrease = Angle.FromDegrees(Math.Max(args.AngleIncrease.Degrees + modSet.AngleIncreaseFlat, 0.0));
        args.AngleDecay = Angle.FromDegrees(Math.Max(args.AngleDecay.Degrees + modSet.AngleDecayFlat, 0.0));
        args.MinAngle = Angle.FromDegrees(Math.Max(args.MinAngle.Degrees + modSet.MinAngleFlat, 0.0));
        args.MaxAngle = Angle.FromDegrees(Math.Max(args.MaxAngle.Degrees + modSet.MaxAngleFlat, args.MinAngle));
        args.ProjectileSpeed += modSet.ProjectileSpeedFlat;

        // Fire delay doesn't work quite like SS14 fire rate, so we're having to do maths:
        // Fire rate is how many shots a weapon can fire in one second.
        // Fire delay is the delay between each shot, expressed here in seconds.
        // First we divide 1 second by the fire rate to get our current fire delay.
        // Then we add the delay modifier to that and divide 1 by the result to get it back to shots per second.
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
