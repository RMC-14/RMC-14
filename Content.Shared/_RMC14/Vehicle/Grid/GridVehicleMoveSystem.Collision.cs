using System;
using System.Numerics;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Foldable;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Vehicle;

public sealed partial class GridVehicleMoverSystem : EntitySystem
{
    private bool CanOccupyTransform(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 gridPos,
        Angle? overrideRotation,
        float clearance)
    {
        if (!physicsQ.TryComp(uid, out var body) || !fixtureQ.TryComp(uid, out var fixtures))
            return true;

        EntityUid? operatorUid = null;
        if (TryComp<VehicleComponent>(uid, out var vehicleComp))
            operatorUid = vehicleComp.Operator;

        if (!body.CanCollide)
            return true;

        if (!gridQ.TryComp(grid, out var gridComp))
            return true;

        var coords = new EntityCoordinates(grid, gridPos);
        var world = coords.ToMap(EntityManager, transform);

        var tileIndices = map.TileIndicesFor(grid, gridComp, coords);
        DebugTestedTiles.Add((grid, tileIndices));

        var rotation = overrideRotation ?? transform.GetWorldRotation(uid);
        var tx = new Transform(world.Position, rotation);

        var wheelDamage = _net.IsClient ? 0f : GetWheelCollisionDamage(uid, mover);

        if (!TryGetFixtureAabb(fixtures, tx, out var aabb))
            return true;

        var hits = lookup.GetEntitiesIntersecting(world.MapId, aabb, LookupFlags.Dynamic | LookupFlags.Static);
        var playedCollisionSound = false;
        var blocked = false;
        var mobHits = new Dictionary<EntityUid, Box2>();

        foreach (var other in hits)
        {
            if (other == uid)
                continue;

            if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
                continue;

            if (!fixtureQ.TryComp(other, out var otherFixtures))
                continue;

            var otherXform = Transform(other);
            var otherTx = physics.GetPhysicsTransform(other, otherXform);

            if (!TryGetFixtureAabb(otherFixtures, otherTx, out var otherAabb))
                continue;

            if (!aabb.Intersects(otherAabb))
                continue;

            var hardCollidable = physics.IsHardCollidable((uid, fixtures, body), (other, otherFixtures, otherBody));
            var skipBlocking = false;

            if (!otherXform.Anchored && HasComp<ItemComponent>(other))
                continue;

            var isBarricade = HasComp<BarricadeComponent>(other);
            var hasDoor = TryComp(other, out DoorComponent? door);
            var isFoldable = HasComp<FoldableComponent>(other);
            var isMob = TryComp(other, out MobStateComponent? mob);
            var isXeno = HasComp<XenoComponent>(other);
            var isVehicle = HasComp<VehicleComponent>(other);
            var isLooseDynamic =
                !otherXform.Anchored &&
                otherBody.BodyType != BodyType.Static &&
                !isMob &&
                !isBarricade &&
                !isFoldable &&
                !isVehicle &&
                !HasComp<RMCVehicleSmashableComponent>(other);

            if (isLooseDynamic)
                continue;

            if (isXeno)
            {
                var blocksXeno = ShouldBlockXeno(mover, other);

                if (blocksXeno)
                {
                    PlayMobCollisionSound(uid, ref playedCollisionSound);
                    ApplyWheelCollisionDamage(uid, mover, wheelDamage);
                    DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, 0f, 0f, clearance, world.MapId));
                    return false;
                }

                PlayMobCollisionSound(uid, ref playedCollisionSound);
                if (!_net.IsClient)
                    PushMobOutOfVehicle(uid, other, aabb, otherAabb);

                continue;
            }

            if (hasDoor && !_net.IsClient)
            {
                _door.TryOpen(other, door, operatorUid);
                if (isBarricade)
                    _door.OnPartialOpen(other, door);
            }

            if (isMob && !isXeno)
                skipBlocking = true;

            if (isBarricade && (hasDoor || isFoldable))
                skipBlocking = true;

            if (isFoldable && !hardCollidable)
                continue;

            if (TrySmash(other, uid, ref playedCollisionSound))
                continue;

            var isBlocked = !skipBlocking && hardCollidable;
            if (isBlocked)
            {
                PlayCollisionSound(uid, ref playedCollisionSound);
                ApplyWheelCollisionDamage(uid, mover, wheelDamage);
                DebugCollisions.Add(new DebugCollision(uid, other, aabb, otherAabb, 0f, 0f, clearance, world.MapId));
                blocked = true;
                break;
            }

            if (!_net.IsClient && isMob && mob != null)
                mobHits[other] = otherAabb;
        }

        if (blocked)
            return false;

        if (!_net.IsClient)
        {
            foreach (var (mobUid, mobAabb) in mobHits)
            {
                if (!TryComp(mobUid, out MobStateComponent? mob))
                    continue;

                HandleMobCollision(uid, mobUid, mob, ref playedCollisionSound);
                if (!HasComp<XenoComponent>(mobUid))
                    PushMobOutOfVehicle(uid, mobUid, aabb, mobAabb);
            }
        }

        return true;
    }

    private void ApplyWheelCollisionDamage(EntityUid vehicle, GridVehicleMoverComponent mover, float damage)
    {
        if (_net.IsClient || damage <= 0f)
            return;

        _wheels.DamageWheels(vehicle, damage);
    }

    private float GetWheelCollisionDamage(EntityUid vehicle, GridVehicleMoverComponent mover)
    {
        if (!TryComp(vehicle, out RMCVehicleWheelSlotsComponent? wheels))
            return 0f;

        var speedMag = MathF.Abs(mover.CurrentSpeed);
        if (speedMag <= 0f)
            return 0f;

        var damage = speedMag * wheels.CollisionDamagePerSpeed;

        if (wheels.MinCollisionDamage > 0f)
            damage = MathF.Max(wheels.MinCollisionDamage, damage);

        return damage;
    }

    private bool ShouldBlockXeno(GridVehicleMoverComponent mover, EntityUid xeno)
    {
        if (mover.XenoBlockMinimumSize is not { } minSize)
            return true;

        if (!_size.TryGetSize(xeno, out var size))
            return true;

        return size >= minSize;
    }

    private bool TryGetFixtureAabb(FixturesComponent fixtures, Transform transformData, out Box2 aabb)
    {
        var first = true;
        aabb = default;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var child = fixture.Shape.ComputeAABB(transformData, i);

                if (first)
                {
                    aabb = child;
                    first = false;
                }
                else
                {
                    aabb = aabb.Union(child);
                }
            }
        }

        return !first;
    }

    private bool TrySmash(EntityUid target, EntityUid vehicle, ref bool playedCollisionSound)
    {
        if (!TryComp(target, out RMCVehicleSmashableComponent? smashable))
            return false;

        PlayCollisionSound(vehicle, ref playedCollisionSound);

        if (TryComp(vehicle, out GridVehicleMoverComponent? mover))
            ApplySmashSlowdown(vehicle, mover, smashable);

        if (!_net.IsClient)
        {
            if (smashable.SmashSound != null)
                _audio.PlayPvs(smashable.SmashSound, vehicle);

            if (smashable.DeleteOnHit && !TerminatingOrDeleted(target))
                Del(target);
        }

        return true;
    }

    private void PlayCollisionSound(EntityUid uid, ref bool played)
    {
        if (played)
            return;

        if (!TryComp<RMCVehicleSoundComponent>(uid, out var sound))
            return;

        if (sound.CollisionSound == null)
            return;

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        if (sound.NextCollisionSound > now)
            return;

        _audio.PlayPvs(sound.CollisionSound, uid);
        sound.NextCollisionSound = now + TimeSpan.FromSeconds(sound.CollisionSoundCooldown);
        Dirty(uid, sound);
        played = true;
    }

    private void PlayMobCollisionSound(EntityUid uid, ref bool played)
    {
        if (played)
            return;

        if (!TryComp<RMCVehicleSoundComponent>(uid, out var sound))
            return;

        var mobSound = sound.MobCollisionSound ?? sound.CollisionSound;
        if (mobSound == null)
            return;

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        if (sound.NextCollisionSound > now)
            return;

        _audio.PlayPvs(mobSound, uid);
        sound.NextCollisionSound = now + TimeSpan.FromSeconds(sound.CollisionSoundCooldown);
        Dirty(uid, sound);
        played = true;
    }

    private void HandleMobCollision(EntityUid vehicle, EntityUid target, MobStateComponent mobState, ref bool playedCollisionSound)
    {
        if (_net.IsClient || _mobState.IsDead(target, mobState))
            return;

        var now = _timing.CurTime;
        if (_lastMobCollision.TryGetValue(target, out var last) && now < last + MobCollisionCooldown)
            return;

        _lastMobCollision[target] = now;

        PlayMobCollisionSound(vehicle, ref playedCollisionSound);

        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                [CollisionDamageType] = MobCollisionDamage,
            },
        };

        _damageable.TryChangeDamage(target, damage);

        if (HasComp<XenoComponent>(target))
            return;

        _stun.TryKnockdown(target, MobCollisionKnockdown, true);

        if (physicsQ.TryComp(target, out var targetBody))
        {
            physics.SetLinearVelocity(target, Vector2.Zero, body: targetBody);
            physics.SetAngularVelocity(target, 0f, body: targetBody);
        }
    }

    private void PushMobOutOfVehicle(EntityUid vehicle, EntityUid mob, Box2 vehicleAabb, Box2 mobAabb)
    {
        var xform = Transform(mob);
        if (xform.Anchored)
            return;

        var vehicleHalf = vehicleAabb.Size / 2f;
        var mobHalf = mobAabb.Size / 2f;

        var vehicleCenter = vehicleAabb.Center;
        var mobCenter = mobAabb.Center;

        var diff = mobCenter - vehicleCenter;
        var overlapX = vehicleHalf.X + mobHalf.X - Math.Abs(diff.X);
        var overlapY = vehicleHalf.Y + mobHalf.Y - Math.Abs(diff.Y);

        if (overlapX <= 0f || overlapY <= 0f)
            return;

        var push = overlapX < overlapY
            ? new Vector2(Math.Sign(diff.X == 0f ? 1f : diff.X) * overlapX, 0f)
            : new Vector2(0f, Math.Sign(diff.Y == 0f ? 1f : diff.Y) * overlapY);

        var pushMultiplier = HasComp<XenoComponent>(mob) ? 2.25f : 1.5f;
        push *= pushMultiplier;

        if (IsPushBlocked(vehicle, mob, mobAabb, push))
            return;

        var newWorldPosition = transform.GetWorldPosition(mob) + push;
        transform.SetWorldPosition(mob, newWorldPosition);

        if (physicsQ.TryComp(mob, out var mobBody))
        {
            physics.SetLinearVelocity(mob, Vector2.Zero, body: mobBody);
            physics.SetAngularVelocity(mob, 0f, body: mobBody);
        }
    }

    private bool IsPushBlocked(EntityUid vehicle, EntityUid mob, Box2 mobAabb, Vector2 push)
    {
        if (push == Vector2.Zero)
            return false;

        var xform = Transform(mob);
        var mapId = xform.MapID;
        if (mapId == MapId.Nullspace)
            return false;

        if (!physicsQ.TryComp(mob, out var mobBody) || !fixtureQ.TryComp(mob, out var mobFixtures))
            return false;

        var shifted = mobAabb.Translated(push);
        var hits = lookup.GetEntitiesIntersecting(mapId, shifted, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var other in hits)
        {
            if (other == mob || other == vehicle)
                continue;

            if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
                continue;

            var otherXform = Transform(other);
            if (otherXform.Anchored || otherBody.BodyType == BodyType.Static)
                return true;

            if (!fixtureQ.TryComp(other, out var otherFixtures))
                continue;

            if (physics.IsHardCollidable((mob, mobFixtures, mobBody), (other, otherFixtures, otherBody)))
                return true;
        }

        return false;
    }
}
