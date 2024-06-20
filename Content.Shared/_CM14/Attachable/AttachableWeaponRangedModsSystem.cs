using Content.Shared._CM14.Attachable.Components;
using Content.Shared._CM14.Attachable.Events;
using Content.Shared._CM14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._CM14.Attachable;

public sealed class AttachableWeaponRangedModsSystem : EntitySystem
{
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableWeaponRangedModsComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
        SubscribeLocalEvent<AttachableWeaponRangedModsToggleableComponent, AttachableAlteredEvent>(OnAttachableWeaponModifiersAltered);
    }

    public void ApplyWeaponModifiers(EntityUid attachableUid, ref GunRefreshModifiersEvent args)
    {
        if (!HasComp<AttachableComponent>(attachableUid))
            return;

        if (TryComp(attachableUid, out AttachableWeaponRangedModsComponent? modifiersComponent))
            ApplyWeaponModifiers(modifiersComponent, ref args);

        if (TryComp(attachableUid,
                out AttachableWeaponRangedModsToggleableComponent? modifiersToggleableComponent))
            ApplyWeaponModifiers(modifiersToggleableComponent, attachableUid, ref args);
    }

    private void ApplyWeaponModifiers(AttachableWeaponRangedModsComponent modifiersComponent,
        ref GunRefreshModifiersEvent args)
    {
        WieldedUnwieldedApplyModifiers(modifiersComponent.Unwielded,
            modifiersComponent.Wielded,
            args.Gun.Owner,
            ref args);
    }

    private void ApplyWeaponModifiers(AttachableWeaponRangedModsToggleableComponent modifiersComponent,
        EntityUid attachableUid,
        ref GunRefreshModifiersEvent args)
    {
        if (!TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent))
            return;

        if (toggleableComponent.Active)
        {
            WieldedUnwieldedApplyModifiers(modifiersComponent.ActiveUnwielded,
                modifiersComponent.ActiveWielded,
                args.Gun.Owner,
                ref args);
            return;
        }

        WieldedUnwieldedApplyModifiers(modifiersComponent.InactiveUnwielded,
            modifiersComponent.InactiveWielded,
            args.Gun.Owner,
            ref args);
    }

    private void WieldedUnwieldedApplyModifiers(
        AttachableWeaponRangedModifierSet modifiers,
        AttachableWeaponRangedModifierSet wieldedModifiers,
        EntityUid holder,
        ref GunRefreshModifiersEvent args)
    {
        if (!TryComp(holder, out WieldableComponent? wieldableComponent) ||
            !wieldableComponent.Wielded)
        {
            ApplyModifierSet(modifiers, ref args);
            return;
        }

        ApplyModifierSet(wieldedModifiers, ref args);
    }

    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        if (!HasComp<GunComponent>(args.Holder))
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                WieldedUnwieldedChangeModifierSet(attachable.Comp.Unwielded, attachable.Comp.Wielded, args.Holder);
                break;

            case AttachableAlteredType.Detached:
                WieldedUnwieldedChangeModifierSet(attachable.Comp.Unwielded,
                    attachable.Comp.Wielded,
                    args.Holder,
                    apply: false);
                break;

            case AttachableAlteredType.Wielded:
                SwapModifierSet(attachable.Comp.Unwielded, attachable.Comp.Wielded, args.Holder);
                break;

            case AttachableAlteredType.Unwielded:
                SwapModifierSet(attachable.Comp.Wielded, attachable.Comp.Unwielded, args.Holder);
                break;
        }
    }

    private void OnAttachableWeaponModifiersAltered(Entity<AttachableWeaponRangedModsToggleableComponent> attachable,
        ref AttachableAlteredEvent args)
    {
        if (!HasComp<GunComponent>(args.Holder))
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                ActiveInactiveChangeModifierSet(attachable, args.Holder);
                break;

            case AttachableAlteredType.Detached:
                ActiveInactiveChangeModifierSet(attachable, args.Holder, apply: false);
                break;

            case AttachableAlteredType.Wielded:
                ActiveInactiveChangeModifierSet(attachable, args.Holder, swapWielded: true);
                break;

            case AttachableAlteredType.Unwielded:
                ActiveInactiveChangeModifierSet(attachable, args.Holder, swapWielded: true);
                break;

            case AttachableAlteredType.Activated:
                ActiveInactiveChangeModifierSet(attachable, args.Holder, apply: false, invertActive: true);
                ActiveInactiveChangeModifierSet(attachable, args.Holder);
                break;

            case AttachableAlteredType.Deactivated:
                ActiveInactiveChangeModifierSet(attachable, args.Holder, apply: false, invertActive: true);
                ActiveInactiveChangeModifierSet(attachable, args.Holder);
                break;
        }
    }

    private void ActiveInactiveChangeModifierSet(
        Entity<AttachableWeaponRangedModsToggleableComponent> attachable,
        EntityUid holder,
        bool apply = true,
        bool invertActive = false,
        bool swapWielded = false)
    {
        if (!TryComp(attachable.Owner, out AttachableToggleableComponent? toggleableComponent))
            return;

        if (toggleableComponent.Active && !invertActive || !toggleableComponent.Active && invertActive)
        {
            WieldedUnwieldedChangeModifierSet(attachable.Comp.ActiveUnwielded,
                attachable.Comp.ActiveWielded,
                holder,
                apply,
                swapWielded);
            return;
        }

        WieldedUnwieldedChangeModifierSet(attachable.Comp.InactiveUnwielded,
            attachable.Comp.InactiveWielded,
            holder,
            apply,
            swapWielded);
    }

    private void WieldedUnwieldedChangeModifierSet(
        AttachableWeaponRangedModifierSet modifiers,
        AttachableWeaponRangedModifierSet wieldedModifiers,
        EntityUid holder,
        bool apply = true,
        bool swap = false)
    {
        if (!TryComp(holder, out WieldableComponent? wieldableComponent) ||
            !wieldableComponent.Wielded)
        {
            if (swap)
            {
                SwapModifierSet(wieldedModifiers, modifiers, holder);
                return;
            }

            ChangeModifierSet(modifiers, holder, apply);
            return;
        }

        if (swap)
        {
            SwapModifierSet(modifiers, wieldedModifiers, holder);
            return;
        }

        ChangeModifierSet(wieldedModifiers, holder, apply);
    }

    private void ApplyModifierSet(AttachableWeaponRangedModifierSet modifierSet, ref GunRefreshModifiersEvent args)
    {
        args.ShotsPerBurst += modifierSet.ShotsPerBurst;

        args.CameraRecoilScalar *= modifierSet.Recoil;
        args.AngleIncrease = new Angle(Math.Max(args.AngleIncrease.Theta * modifierSet.AngleIncrease, 0.0));
        args.AngleDecay = new Angle(Math.Max(args.AngleDecay.Theta * modifierSet.AngleDecay, 0.0));
        args.MinAngle = new Angle(Math.Max(args.MinAngle.Theta * modifierSet.MinAngle, 0.0));
        args.MaxAngle = new Angle(Math.Max(args.MaxAngle.Theta * modifierSet.MaxAngle, args.MinAngle));
        args.FireRate *= modifierSet.FireRate;
        args.ProjectileSpeed *= modifierSet.ProjectileSpeed;
    }

    private void SwapModifierSet(AttachableWeaponRangedModifierSet setToRemove,
        AttachableWeaponRangedModifierSet setToApply,
        EntityUid gunUid)
    {
        ChangeModifierSet(setToRemove, gunUid, apply: false);
        ChangeModifierSet(setToApply, gunUid);
    }

    private void ChangeModifierSet(AttachableWeaponRangedModifierSet modifierSet, EntityUid gunUid, bool apply = true)
    {
        EnsureComp(gunUid, out GunDamageModifierComponent damageModifierComponent);

        if (apply)
        {
            _cmGunSystem.SetGunDamageModifier(damageModifierComponent,
                damageModifierComponent.Multiplier * modifierSet.Damage);
            return;
        }

        _cmGunSystem.SetGunDamageModifier(damageModifierComponent,
            damageModifierComponent.Multiplier / modifierSet.Damage);
    }
}
