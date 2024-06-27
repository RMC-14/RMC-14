using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Shared._RMC14.Attachable;

public sealed class AttachableWeaponMeleeModsSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableWeaponMeleeModsComponent, MeleeHitEvent>(OnMeleeModsHitEvent);

        SubscribeLocalEvent<AttachableWeaponMeleeModsWieldedComponent, MeleeHitEvent>(OnWieldedMeleeModsHitEvent);

        SubscribeLocalEvent<AttachableWeaponMeleeModsToggleableComponent, MeleeHitEvent>(OnToggleableMeleeModsHitEvent);
    }

    private void OnMeleeModsHitEvent(Entity<AttachableWeaponMeleeModsComponent> attachable, ref MeleeHitEvent args)
    {
        ApplyModifierSet(attachable.Comp.Modifiers, ref args);
    }

    private void OnWieldedMeleeModsHitEvent(Entity<AttachableWeaponMeleeModsWieldedComponent> attachable, ref MeleeHitEvent args)
    {
        var modSet = TryComp(attachable, out WieldableComponent? wieldable) && wieldable.Wielded
            ? attachable.Comp.Wielded
            : attachable.Comp.Unwielded;

        ApplyModifierSet(modSet, ref args);
    }

    private void OnToggleableMeleeModsHitEvent(Entity<AttachableWeaponMeleeModsToggleableComponent> attachable, ref MeleeHitEvent args)
    {
        if (!TryComp(attachable, out AttachableToggleableComponent? toggleableComponent))
            return;

        var modSet = TryComp(attachable, out WieldableComponent? wieldable) && wieldable.Wielded
            ? (toggleableComponent.Active ? attachable.Comp.ActiveWielded : attachable.Comp.InactiveWielded)
            : (toggleableComponent.Active ? attachable.Comp.ActiveUnwielded : attachable.Comp.InactiveUnwielded);

        ApplyModifierSet(modSet, ref args);
    }

    private void ApplyModifierSet(AttachableWeaponMeleeModifierSet modSet, ref MeleeHitEvent args)
    {
        if (modSet.BonusDamage != null)
            args.BonusDamage += modSet.BonusDamage;
    }
}
