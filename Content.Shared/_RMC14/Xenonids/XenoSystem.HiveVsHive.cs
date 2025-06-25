using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Xenonids;

public sealed partial class XenoSystem : EntitySystem
{
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

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
