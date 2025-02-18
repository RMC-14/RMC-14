using Content.Shared.Damage;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Weapons.Ranged.HoloTargeting;

public sealed class RMCHoloTargetedSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string HoloKey = "HoloTargeted";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HoloTargetedComponent, DamageModifyEvent>(OnDamageModify);
    }

    /// <summary>
    ///     Try to apply holo stacks to the target up to a certain cap.
    /// </summary>
    public bool TryApplyHoloStacks(EntityUid uid, float duration, float stacks, float maxStacks, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (!_statusEffectsSystem.HasStatusEffect(uid, HoloKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<HoloTargetedComponent>(uid, HoloKey, TimeSpan.FromSeconds(duration), true, status);
        }
        else
        {
            _statusEffectsSystem.TrySetTime(uid, HoloKey, TimeSpan.FromSeconds(duration), status);
        }

        var holoTargeted = EnsureComp<HoloTargetedComponent>(uid);
        var newStacks = holoTargeted.Stacks + stacks;
        holoTargeted.Stacks = Math.Clamp(newStacks, 0f, maxStacks);
        Dirty(uid, holoTargeted);

        return true;
    }

    /// <summary>
    ///     Modify the damage the amount of damage an entity with holo stacks receives.
    /// </summary>
    private void OnDamageModify(EntityUid uid, HoloTargetedComponent component, ref DamageModifyEvent args)
    {
        var damageMultiplier = 1 + component.Stacks / 1000;
        args.Damage *= damageMultiplier;
    }
}
