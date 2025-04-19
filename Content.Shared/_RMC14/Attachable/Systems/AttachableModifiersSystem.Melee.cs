using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    private readonly Dictionary<string, FixedPoint2> _damage = new();

    private void InitializeMelee()
    {
        SubscribeLocalEvent<AttachableWeaponMeleeModsComponent, AttachableGetExamineDataEvent>(OnMeleeModsGetExamineData);
        SubscribeLocalEvent<AttachableWeaponMeleeModsComponent, AttachableRelayedEvent<MeleeHitEvent>>(OnMeleeModsHitEvent);
    }

    private void OnMeleeModsGetExamineData(Entity<AttachableWeaponMeleeModsComponent> attachable, ref AttachableGetExamineDataEvent args)
    {
        foreach (var modSet in attachable.Comp.Modifiers)
        {
            var key = GetExamineKey(modSet.Conditions);

            if (!args.Data.ContainsKey(key))
                args.Data[key] = new (modSet.Conditions, GetEffectStrings(modSet));
            else
                args.Data[key].effectStrings.AddRange(GetEffectStrings(modSet));
        }
    }

    private List<string> GetEffectStrings(AttachableWeaponMeleeModifierSet modSet)
    {
        var result = new List<string>();


        if (modSet.BonusDamage != null)
        {
            var bonusDamage = modSet.BonusDamage.GetTotal();
            if (bonusDamage != 0)
                result.Add(Loc.GetString("rmc-attachable-examine-melee-damage",
                    ("colour", modifierExamineColour), ("sign", bonusDamage > 0 ? '+' : ""), ("damage", bonusDamage)));
        }

        return result;
    }

    private void OnMeleeModsHitEvent(Entity<AttachableWeaponMeleeModsComponent> attachable, ref AttachableRelayedEvent<MeleeHitEvent> args)
    {
        foreach(var modSet in attachable.Comp.Modifiers)
        {
            ApplyModifierSet(attachable, modSet, ref args.Args);
        }
    }

    private void ApplyModifierSet(Entity<AttachableWeaponMeleeModsComponent> attachable, AttachableWeaponMeleeModifierSet modSet, ref MeleeHitEvent args)
    {
        if (!_attachableHolderSystem.TryGetHolder(attachable, out _) ||
            !CanApplyModifiers(attachable.Owner, modSet.Conditions))
        {
            return;
        }

        if (modSet.BonusDamage != null)
            args.BonusDamage += modSet.BonusDamage;

        if (args.BonusDamage.GetTotal() < FixedPoint2.Zero)
        {
            _damage.Clear();
            foreach (var (bonusId, bonusDmg) in args.BonusDamage.DamageDict)
            {
                if (bonusDmg > FixedPoint2.Zero)
                    continue;

                if (!args.BaseDamage.DamageDict.TryGetValue(bonusId, out var baseDamage))
                {
                    _damage[bonusId] = -bonusDmg;
                    continue;
                }

                if (-bonusDmg > baseDamage)
                    _damage[bonusId] = -bonusDmg - baseDamage;
            }

            foreach (var (bonusId, bonusDmg) in _damage)
            {
                args.BonusDamage.DamageDict[bonusId] = -bonusDmg;
            }
        }
    }
}
