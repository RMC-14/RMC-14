using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Barricade;

public abstract class SharedDirectionalAttackBlockSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom Random = default!;
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
            if (!TryComp(target, out DirectionalAttackBlockerComponent? blocker) || !Transform(target).Anchored)
                continue;

            if (!IsFacingTarget(ent, target))
                continue;

            if (TryComp(target, out DamageableComponent? damageable))
            {
                var blockChance = Math.Max(blocker.MinimumBlockChance, (blocker.MaxHealth - (float) damageable.TotalDamage) / blocker.MaxHealth);

                // Calculate a new block chance for the barricade, the event should only be handled server side because of randomness.
                if (blockChance <= blocker.BlockRoll)
                {
                    var ev = new FailedBlockAttemptEvent();
                    RaiseLocalEvent(target, ref ev);
                    continue;
                }
            }

            // The attack has been blocked, swap the attack target to the blocking entity.
            switch (args.Attack)
            {
                case LightAttackEvent light:
                    args.Attack = new LightAttackEvent(potentialTarget, args.Weapon, light.Coordinates);
                    break;
                // A disarm attempt is turned into a light attack on the blocking entity.
                case DisarmAttackEvent disarm:
                    args.Attack = new LightAttackEvent(potentialTarget, args.Weapon, disarm.Coordinates);
                    break;
            }
            break;
        }
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
        var targetCoordinates = _transform.GetMoverCoordinates(target);
        if (originCoordinates != null)
            targetCoordinates = originCoordinates.Value;

        var blockerCoordinates = _transform.GetMoverCoordinateRotation(blocker, Transform(blocker));
        var diff = targetCoordinates.Position - blockerCoordinates.Coords.Position;
        var dir = diff.Normalized().GetDir();
        var blockerDirection = blockerCoordinates.worldRot.GetDir();
        var relativeDiff = Math.Abs(dir - blockerDirection);

        // Only block if the leap originates from a location that is at most one ordinal direction away from the direction the blocker is facing (135 degree cone).
        // For example, if the blocker is facing North, the leap will be blocked if it originates from a position to the North-West, North, or North-East of the blocker.
        return relativeDiff is 0 or 1 or 7;
    }
}

[ByRefEvent]
[Serializable, NetSerializable]
public record struct FailedBlockAttemptEvent;
