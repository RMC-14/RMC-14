using Content.Shared._CM14.Attachable.Components;
using Content.Shared._CM14.Attachable.Events;
using Content.Shared._CM14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._CM14.Attachable;

public sealed class AttachableWeaponRangedModsSystem : EntitySystem
{
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, GunRefreshModifiersEvent>(OnAttachableWeaponRangedModsRefreshModifiers);
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsToggleableComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
    }

    private void OnAttachableWeaponRangedModsRefreshModifiers(Entity<AttachableWeaponRangedModsComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var set = TryComp(ent, out WieldableComponent? wieldable) && wieldable.Wielded
            ? ent.Comp.Wielded
            : ent.Comp.Unwielded;

        args.ShotsPerBurst += set.ShotsPerBurst;
        args.CameraRecoilScalar *= set.Recoil;
        args.AngleIncrease = new Angle(Math.Max(args.AngleIncrease.Theta * set.AngleIncrease, 0.0));
        args.AngleDecay = new Angle(Math.Max(args.AngleDecay.Theta * set.AngleDecay, 0.0));
        args.MinAngle = new Angle(Math.Max(args.MinAngle.Theta * set.MinAngle, 0.0));
        args.MaxAngle = new Angle(Math.Max(args.MaxAngle.Theta * set.MaxAngle, args.MinAngle));
        args.FireRate *= set.FireRate;
        args.ProjectileSpeed *= set.ProjectileSpeed;
    }

    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);
    }

    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        _cmGunSystem.RefreshGunDamageMultiplier(args.Holder);
    }
}
