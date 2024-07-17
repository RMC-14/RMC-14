using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeRanged()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GunRefreshModifiersEvent>>(OnRangedModsRefreshModifiers);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableAlteredEvent>(OnRangedModsAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GetGunDamageModifierEvent>>(OnRangedModsGetGunDamage);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableRelayedEvent<GetFireModeValuesEvent>>(OnRangedModsGetFireModeValues);
    }

    private void OnRangedModsRefreshModifiers(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GunRefreshModifiersEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.ShotsPerBurst += modSet.ShotsPerBurstFlat;
            args.Args.CameraRecoilScalar = Math.Max(args.Args.CameraRecoilScalar + modSet.RecoilFlat, 0);
            args.Args.MinAngle = Angle.FromDegrees(Math.Max(args.Args.MinAngle.Degrees + modSet.ScatterFlat, 0.0));
            args.Args.MaxAngle = Angle.FromDegrees(Math.Max(args.Args.MaxAngle.Degrees + modSet.ScatterFlat, args.Args.MinAngle));
            args.Args.ProjectileSpeed += modSet.ProjectileSpeedFlat;

            // Fire delay doesn't work quite like SS14 fire rate, so we're having to do maths:
            // Fire rate is shots per second. Fire delay is the interval between shots. They are inversely proportionate to each other.
            // First we divide 1 second by the fire rate to get our current fire delay, then we add the delay modifier, then we divide 1 by the result again to get the modified fire rate.
            var fireDelayMod = args.Args.Gun.Comp.SelectedMode == SelectiveFire.Burst ? modSet.FireDelayFlat / 2f : modSet.FireDelayFlat;
            args.Args.FireRate = 1f / (1f / args.Args.FireRate + fireDelayMod);
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
                _rmcSelectiveFireSystem.RefreshModifiableFireModeValues(args.Holder);
                //_gunSystem.RefreshModifiers(args.Holder);
                break;
        }
    }

    private void OnRangedModsGetGunDamage(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GetGunDamageModifierEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.Multiplier += modSet.DamageAddMult;
        }
    }

    private void OnRangedModsGetFireModeValues(Entity<AttachableWeaponRangedModsComponent> attachable, ref AttachableRelayedEvent<GetFireModeValuesEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
                continue;

            args.Args.BurstScatterMult += modSet.BurstScatterAddMult;
        }
    }
}
