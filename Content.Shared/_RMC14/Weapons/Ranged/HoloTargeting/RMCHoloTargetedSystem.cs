using Content.Shared.Damage;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Weapons.Ranged.HoloTargeting;

public sealed class RMCHoloTargetedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HoloTargetedComponent, DamageModifyEvent>(OnDamageModify);
    }

    /// <summary>
    ///     Try to apply holo stacks to the target up to a certain cap.
    /// </summary>
    public void ApplyHoloStacks(EntityUid uid, float decay, float stacks, float maxStacks)
    {
        var holoTargeted = EnsureComp<HoloTargetedComponent>(uid);
        holoTargeted.Decay = decay;
        var newStacks = holoTargeted.Stacks + stacks;
        holoTargeted.Stacks = Math.Clamp(newStacks, 0f, maxStacks);
        Dirty(uid, holoTargeted);
    }

    /// <summary>
    ///     Modify the damage the amount of damage an entity with holo stacks receives.
    /// </summary>
    private void OnDamageModify(EntityUid uid, HoloTargetedComponent component, ref DamageModifyEvent args)
    {
        var damageMultiplier = 1 + component.Stacks / 1000;
        args.Damage *= damageMultiplier;
    }

    /// <summary>
    ///     Reduce the amount of holo stacks every second and remove the component if the amount of stacks reaches 0.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<HoloTargetedComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            component.DecayTimer += frameTime;
            if (component.DecayTimer >= 1)
            {
                component.DecayTimer = 0f;
                component.Stacks -= component.Decay;
                Dirty(uid, component);
                if (component.Stacks <= 0)
                    RemCompDeferred<HoloTargetedComponent>(uid);
            }
        }
    }
}
