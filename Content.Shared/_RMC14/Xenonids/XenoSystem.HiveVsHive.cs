using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.ManageHive.Boons;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids;

public sealed partial class XenoSystem : EntitySystem
{
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    private static readonly ProtoId<DamageTypePrototype>[] AggressionBruteDamageTypes = ["Blunt", "Slash", "Piercing"];
    private static readonly ProtoId<DamageTypePrototype> AggressionStructuralDamageType = "Structural";

    public const float XENO_SLASH_DAMAGE_MULT = 1.5f;
    public const float XENO_DEBUFF_MULT = 1.25f;
    public const float XENO_ACID_DAMAGE_MULT = 2.625f;
    public const float XENO_PROJECTILE_DAMAGE_MULT = 2.625f;

    public DamageSpecifier TryApplyXenoSlashDamageMultiplier(EntityUid target, DamageSpecifier baseDamage)
    {
        if (!_size.TryGetSize(target, out var size) || !_size.IsXenoSized(size))
            return baseDamage;

        return baseDamage * XENO_SLASH_DAMAGE_MULT;
    }

    public DamageSpecifier ApplyXenoAggressionDamage(EntityUid attacker, DamageSpecifier baseDamage)
    {
        if (!_hiveBoon.TryGetActiveBoon<HiveBoonAggressionComponent>(attacker, out var boon))
            return new DamageSpecifier(baseDamage);

        return ApplyXenoAggressionDamageBonus(baseDamage, boon.Comp.Damage);
    }

    public DamageSpecifier ApplyXenoMeleeDamageModifiers(EntityUid attacker, EntityUid target, DamageSpecifier baseDamage)
    {
        return TryApplyXenoSlashDamageMultiplier(target, ApplyXenoAggressionDamage(attacker, baseDamage));
    }

    public static DamageSpecifier ApplyXenoAggressionDamageBonus(DamageSpecifier baseDamage, FixedPoint2 amount)
    {
        var damage = new DamageSpecifier(baseDamage);
        AddPhysicalMeleeDamageBonus(damage, amount);
        return damage;
    }

    private static void AddPhysicalMeleeDamageBonus(DamageSpecifier damage, FixedPoint2 amount)
    {
        if (amount <= FixedPoint2.Zero)
            return;

        var bruteTotal = FixedPoint2.Zero;
        string? lastBruteType = null;
        var bruteTypes = 0;

        foreach (var type in AggressionBruteDamageTypes)
        {
            if (!damage.DamageDict.TryGetValue(type, out var value) ||
                value <= FixedPoint2.Zero)
            {
                continue;
            }

            bruteTotal += value;
            lastBruteType = type;
            bruteTypes++;
        }

        if (bruteTotal > FixedPoint2.Zero &&
            lastBruteType != null)
        {
            if (bruteTypes == 1)
            {
                damage.DamageDict[lastBruteType] += amount;
                return;
            }

            var remaining = amount;
            foreach (var type in AggressionBruteDamageTypes)
            {
                if (!damage.DamageDict.TryGetValue(type, out var value) ||
                    value <= FixedPoint2.Zero)
                {
                    continue;
                }

                if (type == lastBruteType)
                {
                    damage.DamageDict[type] += remaining;
                    return;
                }

                var added = amount * value / bruteTotal;
                damage.DamageDict[type] += added;
                remaining -= added;
            }

            return;
        }

        if (damage.DamageDict.TryGetValue(AggressionStructuralDamageType, out var structural) &&
            structural > FixedPoint2.Zero)
        {
            damage.DamageDict[AggressionStructuralDamageType] = structural + amount;
        }
    }

    public TimeSpan TryApplyXenoDebuffMultiplier(EntityUid target, TimeSpan baseDuration)
    {
        if (!_size.TryGetSize(target, out var size) || !_size.IsXenoSized(size))
            return baseDuration;

        return baseDuration * XENO_DEBUFF_MULT;
    }

    public DamageSpecifier TryApplyXenoAcidDamageMultiplier(EntityUid target, DamageSpecifier baseDamage)
    {
        if (!HasComp<XenoComponent>(target))
            return baseDamage;

        return baseDamage * XENO_ACID_DAMAGE_MULT;
    }

    public DamageSpecifier TryApplyXenoProjectileDamageMultiplier(EntityUid target, DamageSpecifier baseDamage)
    {
        if (!HasComp<XenoComponent>(target))
            return baseDamage;

        return baseDamage * XENO_PROJECTILE_DAMAGE_MULT;
    }
}
