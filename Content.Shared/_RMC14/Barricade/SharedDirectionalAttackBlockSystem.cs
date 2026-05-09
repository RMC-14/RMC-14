using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Barricade;

public abstract class SharedDirectionalAttackBlockSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, MeleeAttackAttemptEvent>(OnMeleeAttackAttempt);
    }

    private void OnMeleeAttackAttempt(Entity<MobStateComponent> ent, ref MeleeAttackAttemptEvent args)
    {
        foreach (var potentialTarget in args.PotentialTargets)
        {
            if(potentialTarget == args.Target)
                break;

            var target = GetEntity(potentialTarget);

            if (!IsAttackBlocked(ent, target))
                continue;

            var newCoordinates = GetNetCoordinates(_transform.GetMoverCoordinates(GetEntity(potentialTarget)));

            // The attack has been blocked, swap the attack target to the blocking entity.
            switch (args.Attack)
            {
                case LightAttackEvent light:
                    args.Attack = new LightAttackEvent(potentialTarget, args.Weapon, newCoordinates);
                    break;
                // A disarm attempt is turned into a light attack on the blocking entity.
                case DisarmAttackEvent disarm:
                    args.Attack = new LightAttackEvent(potentialTarget, args.Weapon, newCoordinates);
                    break;
            }
            break;
        }
    }

    /// <summary>
    ///     Check if the attack is blocked by the target.
    /// </summary>
    /// <param name="attacker">The entity doing the attack</param>
    /// <param name="target">The target of the attack</param>
    /// <returns>True if the attack is blocked</returns>
    public bool IsAttackBlocked(EntityUid attacker, EntityUid target)
    {
        if (!TryComp(target, out DirectionalAttackBlockerComponent? blocker) || !Transform(target).Anchored)
            return false;

        if (!blocker.BlockMarineAttacks && HasComp<MarineComponent>(attacker))
            return false;

        if (!IsFacingTarget(target, attacker))
            return false;

        var tick = _timing.CurTick.Value;
        var iD = GetNetEntity(attacker).Id;
        var seed = ((long)tick << 32) | (uint)iD;

        if (TryComp(target, out DamageableComponent? damageable))
        {
            var blockChance = Math.Max(blocker.MinimumBlockChance, (blocker.MaxHealth - (float) damageable.TotalDamage) / blocker.MaxHealth);
            var blockRoll = new Xoroshiro64S(seed).NextFloat(0, 1);

            if (blockChance < blockRoll)
                return false;
        }
        return true;
    }

    private sbyte GetRelativeDiff(EntityUid blocker, EntityUid target, EntityCoordinates? originCoordinates = null)
    {
        var targetCoordinates = originCoordinates ?? _transform.GetMoverCoordinates(target);

        var blockerCoordinates = _transform.GetMoverCoordinateRotation(blocker, Transform(blocker));
        var diff = targetCoordinates.Position - blockerCoordinates.Coords.Position;
        var dir = diff.Normalized().GetDir();
        var blockerDirection = blockerCoordinates.worldRot.GetDir();
        var relativeDiff = Math.Abs(dir - blockerDirection);

        return relativeDiff;
    }

    /// <summary>
    ///     Check if the blocker is facing towards the target.
    /// </summary>
    /// <param name="blocker">The entity whose direction is checked.</param>
    /// <param name="target">The entity that is checked to see if it is in front of the blocker</param>
    /// <param name="originCoordinates">The target coordinates to check, if left empty the targets current coordinates will be used</param>
    /// <returns>True if the blocker is facing the target</returns>
    public bool IsFacingTarget(EntityUid blocker, EntityUid target, EntityCoordinates? originCoordinates = null)
    {
        var relativeDiff = GetRelativeDiff(blocker, target, originCoordinates);

        // Only block if the leap originates from a location that is at most one ordinal direction away from the direction the blocker is facing (135 degree cone).
        // For example, if the blocker is facing North, the leap will be blocked if it originates from a position to the North-West, North, or North-East of the blocker.
        return relativeDiff is 0 or 1 or 7; // Front
    }

    /// <summary>
    ///     Check if the blocker is behind the target.
    /// </summary>
    /// <param name="blocker">The entity whose direction is checked.</param>
    /// <param name="target">The entity that is checked to see if it is behind the blocker</param>
    /// <param name="originCoordinates">The target coordinates to check, if left empty the targets current coordinates will be used</param>
    /// <returns>True if the blocker is behind the target</returns>
    public bool IsBehindTarget(EntityUid blocker, EntityUid target, EntityCoordinates? originCoordinates = null)
    {
        var relativeDiff = GetRelativeDiff(blocker, target, originCoordinates);

        // Only block if the leap originates from a location that is at most one ordinal direction away from the direction the blocker is facing (135 degree cone).
        // For example, if the blocker is facing North, the leap will be blocked if it originates from a position to the South-West, South, or South-East of the blocker.
        return relativeDiff is 3 or 4 or 5; // Opposite directions
    }
}
