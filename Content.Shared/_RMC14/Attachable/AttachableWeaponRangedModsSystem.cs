using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._RMC14.Attachable;

public sealed class AttachableWeaponRangedModsSystem : EntitySystem
{
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, GunRefreshModifiersEvent>(OnRangedModsRefreshModifiers);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableAlteredEvent>(OnRangedModsAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, GetGunDamageModifierEvent>(OnRangedModsGetGunDamage);

        SubscribeLocalEvent<AttachableWeaponWieldedRangedModsComponent, GunRefreshModifiersEvent>(OnWieldedRangedModsRefreshModifiers);
        SubscribeLocalEvent<AttachableWeaponWieldedRangedModsComponent, AttachableAlteredEvent>(OnWieldedRangedModsAttachableAltered);
        SubscribeLocalEvent<AttachableWeaponWieldedRangedModsComponent, GetGunDamageModifierEvent>(OnWieldedRangedModsGetGunDamage);

        SubscribeLocalEvent<AttachableWeaponRangedModsToggleableComponent, AttachableAlteredEvent>(OnWeaponModifiersAltered);
    }

    private void OnRangedModsRefreshModifiers(Entity<AttachableWeaponRangedModsComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var modSet = ent.Comp.Modifiers;

        args.ShotsPerBurst += modSet.ShotsPerBurstFlat;
        args.CameraRecoilScalar += modSet.RecoilFlat;
        args.AngleIncrease = Angle.FromDegrees(Math.Max(args.AngleIncrease.Degrees + modSet.AngleIncreaseFlat, 0.0));
        args.AngleDecay = Angle.FromDegrees(Math.Max(args.AngleDecay.Degrees + modSet.AngleDecayFlat, 0.0));
        args.MinAngle = Angle.FromDegrees(Math.Max(args.MinAngle.Degrees + modSet.MinAngleFlat, 0.0));
        args.MaxAngle = Angle.FromDegrees(Math.Max(args.MaxAngle.Degrees + modSet.MaxAngleFlat, args.MinAngle));
        args.FireRate = Math.Max(args.FireRate + modSet.FireRateFlat, 0.1f);
        args.ProjectileSpeed += modSet.ProjectileSpeedFlat;
    }

    private void OnRangedModsAltered(Entity<AttachableWeaponRangedModsComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);
    }

    private void OnRangedModsGetGunDamage(Entity<AttachableWeaponRangedModsComponent> ent, ref GetGunDamageModifierEvent args)
    {
        args.Multiplier += ent.Comp.Modifiers.DamageAddMult;
    }

    private void OnWieldedRangedModsRefreshModifiers(Entity<AttachableWeaponWieldedRangedModsComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var modSet = TryComp(ent, out WieldableComponent? wieldable) && wieldable.Wielded
            ? ent.Comp.Wielded
            : ent.Comp.Unwielded;

        args.ShotsPerBurst += modSet.ShotsPerBurstFlat;
        args.CameraRecoilScalar += modSet.RecoilFlat;
        args.AngleIncrease = Angle.FromDegrees(Math.Max(args.AngleIncrease.Degrees + modSet.AngleIncreaseFlat, 0.0));
        args.AngleDecay = Angle.FromDegrees(Math.Max(args.AngleDecay.Degrees + modSet.AngleDecayFlat, 0.0));
        args.MinAngle = Angle.FromDegrees(Math.Max(args.MinAngle.Degrees + modSet.MinAngleFlat, 0.0));
        args.MaxAngle = Angle.FromDegrees(Math.Max(args.MaxAngle.Degrees + modSet.MaxAngleFlat, args.MinAngle));
        args.FireRate = Math.Max(args.FireRate + modSet.FireRateFlat, 0.1f);
        args.ProjectileSpeed += modSet.ProjectileSpeedFlat;
    }

    private void OnWieldedRangedModsAttachableAltered(Entity<AttachableWeaponWieldedRangedModsComponent> ent, ref AttachableAlteredEvent args)
    {
        _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);
    }

    private void OnWieldedRangedModsGetGunDamage(Entity<AttachableWeaponWieldedRangedModsComponent> ent, ref GetGunDamageModifierEvent args)
    {
        var modSet = TryComp(ent, out WieldableComponent? wieldable) && wieldable.Wielded
            ? ent.Comp.Wielded
            : ent.Comp.Unwielded;

        args.Multiplier += modSet.DamageAddMult;
    }

    private void OnWeaponModifiersAltered(Entity<AttachableWeaponRangedModsToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);
    }
}
