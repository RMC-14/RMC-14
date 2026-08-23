using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Barricade;

public abstract class SharedDirectionalAttackBlockSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, MeleeAttackAttemptEvent>(OnMeleeAttackAttempt);
        SubscribeLocalEvent<ProjectileCoverComponent, PreventCollideEvent>(OnBarricadePreventCollide);
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

    private void OnBarricadePreventCollide(Entity<ProjectileCoverComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(args.OtherEntity, out ProjectileComponent? projectile))
            return;

        if (!Transform(ent).Anchored)
            return;

        var barricadeNet = GetNetEntity(ent.Owner);

        if (TryComp(args.OtherEntity, out ProjectileCoverPassedComponent? passed) && passed.Barricades.Contains(barricadeNet))
        {
            args.Cancelled = true;
            return;
        }

        // Shots aimed at barricade hit it
        if (TryComp(args.OtherEntity, out TargetedProjectileComponent? targeted) && targeted.Target == ent.Owner)
            return;

        if (TryComp(args.OtherEntity, out ProjectileCoverInteractionComponent? interaction) && interaction.IgnoreCover)
        {
            args.Cancelled = true;
            MarkPassed(args.OtherEntity, barricadeNet);
            return;
        }

        if (!TryComp(args.OtherEntity, out RMCProjectileAccuracyComponent? accuracy) || accuracy.ShotFrom is not { } shotFrom)
            return;

        var facing = IsFacingTarget(ent.Owner, args.OtherEntity, shotFrom);
        var behind = !facing && IsBehindTarget(ent.Owner, args.OtherEntity, shotFrom);
        if (!facing && !behind)
        {
            args.Cancelled = true;
            MarkPassed(args.OtherEntity, barricadeNet);
            return;
        }

        var currentCoords = _transform.GetMoverCoordinates(args.OtherEntity);
        var distance = (currentCoords.Position - shotFrom.Position).Length();

        if (facing)
        {
            if (interaction is { StoppedByCover: true })
                return;

            distance -= 1;
        }
        else if (distance > 3)
        {
            args.Cancelled = true;
            MarkPassed(args.OtherEntity, barricadeNet);
            return;
        }

        if (distance < 1)
        {
            args.Cancelled = true;
            MarkPassed(args.OtherEntity, barricadeNet);
            return;
        }

        // accuracy can go over 100 (idk)
        var accuracyValue = Math.Clamp((float) accuracy.Accuracy, 0f, 100f);
        var coverage = (float) ent.Comp.Coverage;
        var hitChance = Math.Min(coverage, coverage * distance / 6f + 50f * (1f - accuracyValue / 100f));

        var tick = _timing.CurTick.Value;
        var barricadeId = (long) barricadeNet.Id;
        var roll = new Xoshiro128P(accuracy.GunSeed, ((long) tick << 32) | barricadeId).NextFloat(0, 100);

        if (roll >= hitChance)
        {
            args.Cancelled = true;
            MarkPassed(args.OtherEntity, barricadeNet);
        }
    }

    private void MarkPassed(EntityUid projectile, NetEntity barricadeNet)
    {
        var passed = EnsureComp<ProjectileCoverPassedComponent>(projectile);
        passed.Barricades.Add(barricadeNet);
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

    public bool IsDirectionBlocked(EntityUid origin, AtmosDirection cardinal, float checkRange = 0.6f, CollisionGroup collisionGroup = CollisionGroup.BarricadeImpassable | CollisionGroup.BulletImpassable)
    {
        return IsDirectionBlocked(origin, (Vector2)cardinal.CardinalToIntVec(), checkRange, collisionGroup);
    }

    public bool IsDirectionBlocked(EntityUid origin, Vector2 direction, float checkRange = 0.6f, CollisionGroup collisionGroup = CollisionGroup.BarricadeImpassable | CollisionGroup.BulletImpassable)
    {
        if (direction == Vector2.Zero)
            return false;

        var originPosition = _transform.GetMoverCoordinates(origin).Position;
        var ray = new CollisionRay(originPosition, direction, (int)collisionGroup);
        var intersect = _physics.IntersectRayWithPredicate(Transform(origin).MapID, ray, checkRange, e => !Transform(e).Anchored);
        return intersect.Any();
    }
}
