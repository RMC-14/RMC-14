using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Xenonids.Hive;

/// <summary>
/// applies the stats carried by <see cref="HiveStatModifierComponent"/>.
/// </summary>
public sealed class HiveStatModifierSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiveStatModifierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HiveStatModifierComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<HiveStatModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private void OnMapInit(Entity<HiveStatModifierComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.HealthMultiplier != 1f && TryComp(ent, out MobThresholdsComponent? thresholds))
        {
            foreach (var (threshold, state) in new List<KeyValuePair<FixedPoint2, MobState>>(thresholds.Thresholds))
            {
                if (state is MobState.Critical or MobState.Dead)
                    _mobThreshold.SetMobStateThreshold(ent, threshold * ent.Comp.HealthMultiplier, state, thresholds);
            }
        }

        if (ent.Comp.SpeedMultiplier != 1f)
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnGetMeleeDamage(Entity<HiveStatModifierComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage += ent.Comp.DamageIncrease;
    }

    private void OnRefreshMovementSpeed(Entity<HiveStatModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.SpeedMultiplier != 1f)
            args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }
}
