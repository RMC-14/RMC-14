using Content.Shared._RMC14.Attachable.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private void InitializeMelee()
    {
        SubscribeLocalEvent<AttachableWeaponMeleeModsComponent, MeleeHitEvent>(OnMeleeModsHitEvent);
    }

    private void OnMeleeModsHitEvent(Entity<AttachableWeaponMeleeModsComponent> attachable, ref MeleeHitEvent args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args);
        }
    }

    private void ApplyModifierSet(Entity<AttachableWeaponMeleeModsComponent> attachable, AttachableWeaponMeleeModifierSet modSet, ref MeleeHitEvent args)
    {
        if (!CanApplyModifiers(attachable.Owner, modSet.Conditions))
            return;

        if (modSet.BonusDamage != null)
            args.BonusDamage += modSet.BonusDamage;
    }
}
