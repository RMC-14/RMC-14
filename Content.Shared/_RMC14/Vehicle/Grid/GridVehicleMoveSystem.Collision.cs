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
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Collections;

namespace Content.Shared.Vehicle;

public sealed partial class GridVehicleMoverSystem : EntitySystem
{
    private enum CollisionHandlingResult : byte
    {
        Continue = 0,
        Blocked = 1,
    }

    private readonly record struct CollisionCandidate(
        EntityUid Entity,
        Box2 Aabb,
        Box2 CollisionAabb,
        VehicleCollisionClass CollisionClass,
        DoorComponent? Door,
        MobStateComponent? MobState,
        bool IsBarricade,
        bool IsXeno,
        bool IsVehicle,
        bool IsUnpoweredDoor);

    private bool CanOccupyTransform(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 gridPos,
        Angle? overrideRotation,
        float clearance,
        bool applyEffects,
        bool debug = true,
        HashSet<EntityUid>? blockers = null,
        HashSet<EntityUid>? ignoredEntities = null)
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

        var debugEnabled = debug && CollisionDebugEnabled;
        if (debugEnabled)
        {
            var tileIndices = map.TileIndicesFor(grid, gridComp, coords);
            DebugTestedTiles.Add((grid, tileIndices));
        }

        var rotation = GetCollisionWorldRotation(uid, grid, overrideRotation);
        var tx = new Transform(world.Position, rotation);

        var wheelDamage = _net.IsClient ? 0f : GetWheelCollisionDamage(uid, mover);

        if (!TryGetFixtureAabb(fixtures, tx, out var aabb))
            return true;

        var movementAabb = GetMovementAabb(aabb, mover);
        var hits = lookup.GetEntitiesIntersecting(world.MapId, aabb, LookupFlags.Dynamic | LookupFlags.Static);
        var playedCollisionSound = false;
        var mobHits = new ValueList<EntityUid>(0);

        void AddProbe(bool probeBlocked)
        {
            if (!debugEnabled)
                return;

            AddDebugCollisionProbe(uid, mover, fixtures, tx, aabb, movementAabb, world.MapId, probeBlocked, applyEffects);
        }

        foreach (var other in hits)
        {
            if (other == uid)
                continue;

            if (TryComp(other, out VehicleRideSurfaceRiderComponent? rider) && rider.Vehicle == uid)
                continue;

            if (ignoredEntities != null && ignoredEntities.Contains(other))
                continue;

            if (!TryBuildCollisionCandidate(
                    uid,
                    fixtures,
                    body,
                    other,
                    aabb,
                    movementAabb,
                    operatorUid,
                    out var candidate))
            {
                continue;
            }

            if (candidate.CollisionClass == VehicleCollisionClass.SoftMob && candidate.IsXeno)
            {
                var result = HandleSoftXenoCollision(
                    uid,
                    mover,
                    grid,
                    world.Position,
                    world.MapId,
                    candidate.Entity,
                    aabb,
                    candidate.Aabb,
                    candidate.CollisionAabb,
                    clearance,
                    applyEffects,
                    debugEnabled,
                    blockers,
                    wheelDamage,
                    ref playedCollisionSound);

                if (result == CollisionHandlingResult.Blocked)
                {
                    AddProbe(true);
                    return false;
                }

                continue;
            }

            if (candidate.CollisionClass == VehicleCollisionClass.SoftMob &&
                candidate.MobState != null &&
                _standing.IsDown(candidate.Entity))
            {
                continue;
            }

            if (applyEffects && candidate.Door is { } door && !_net.IsClient)
            {
                if (!candidate.IsUnpoweredDoor)
                {
                    _door.TryOpen(candidate.Entity, door, operatorUid);
                    if (candidate.IsBarricade)
                        _door.OnPartialOpen(candidate.Entity, door);
                }
            }

            if (candidate.CollisionClass == VehicleCollisionClass.Ignore)
                continue;

            if (candidate.CollisionClass == VehicleCollisionClass.Breakable)
            {
                var result = HandleBreakableCollision(
                    uid,
                    mover,
                    candidate.Entity,
                    candidate.CollisionAabb,
                    candidate.Aabb,
                    clearance,
                    world.MapId,
                    candidate.Door != null,
                    candidate.IsUnpoweredDoor,
                    applyEffects,
                    debugEnabled,
                    blockers,
                    wheelDamage,
                    ref playedCollisionSound);

                if (result == CollisionHandlingResult.Blocked)
                {
                    AddProbe(true);
                    return false;
                }

                continue;
            }

            if (candidate.CollisionClass == VehicleCollisionClass.Hard)
            {
                var result = HandleHardCollision(
                    uid,
                    mover,
                    grid,
                    gridPos,
                    candidate.Entity,
                    candidate.CollisionAabb,
                    candidate.Aabb,
                    clearance,
                    world.MapId,
                    candidate.IsVehicle,
                    applyEffects,
                    debugEnabled,
                    blockers,
                    wheelDamage,
                    ref playedCollisionSound);

                if (result == CollisionHandlingResult.Blocked)
                {
                    AddProbe(true);
                    return false;
                }

                continue;
            }

            if (applyEffects &&
                _net.IsClient &&
                !candidate.IsXeno &&
                candidate.MobState != null &&
                ShouldPredictVehicleInteractions(uid))
            {
                PredictRunover(uid, candidate.Entity, candidate.MobState);
            }

            if (applyEffects && !_net.IsClient && candidate.MobState != null)
            {
                if (!mobHits.Contains(candidate.Entity))
                    mobHits.Add(candidate.Entity);
            }
        }

        if (!_net.IsClient && mobHits.Count > 0)
        {
            foreach (var mobUid in mobHits)
            {
                if (!TryComp(mobUid, out MobStateComponent? mob))
                    continue;

                HandleMobCollision(uid, mobUid, mob, ref playedCollisionSound);
            }
        }

        AddProbe(false);
        return true;
    }

    private bool TryBuildCollisionCandidate(
        EntityUid vehicle,
        FixturesComponent vehicleFixtures,
        PhysicsComponent vehicleBody,
        EntityUid other,
        Box2 vehicleAabb,
        Box2 movementAabb,
        EntityUid? operatorUid,
        out CollisionCandidate candidate)
    {
        candidate = default;

        var otherXform = Transform(other);
        if (!otherXform.Anchored && HasComp<ItemComponent>(other))
            return false;

        if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
            return false;

        var hasDoor = TryComp(other, out DoorComponent? door);
        var isBarricade = HasComp<BarricadeComponent>(other);
        var isFoldable = HasComp<FoldableComponent>(other);
        var isMob = TryComp(other, out MobStateComponent? mob);
        var isXeno = HasComp<XenoComponent>(other);
        var isVehicle = HasComp<VehicleComponent>(other);
        var isSmashable = HasComp<VehicleSmashableComponent>(other);

        if (!isMob &&
            !isXeno &&
            !otherXform.Anchored &&
            otherBody.BodyType != BodyType.Static &&
            !isBarricade &&
            !isFoldable &&
            !isVehicle &&
            !isSmashable)
        {
            return false;
        }

        if (!fixtureQ.TryComp(other, out var otherFixtures))
            return false;

        var otherTx = physics.GetPhysicsTransform(other, otherXform);

        if (!TryGetFixtureAabb(otherFixtures, otherTx, out var otherAabb))
            return false;

        if (!vehicleAabb.Intersects(otherAabb))
            return false;

        var hardCollidable = physics.IsHardCollidable((vehicle, vehicleFixtures, vehicleBody), (other, otherFixtures, otherBody));
        var collisionClass = ClassifyCollisionCandidate(
            other,
            otherXform,
            otherBody,
            otherFixtures,
            hardCollidable,
            isMob,
            isBarricade,
            isFoldable,
            hasDoor,
            isXeno,
            isVehicle,
            isSmashable);

        var doorPowerKnown = TryGetDoorPowered(other, out var doorPowered);
        var isUnpoweredDoor = hasDoor && doorPowerKnown && !doorPowered;
        if (hasDoor && doorPowerKnown && doorPowered && door != null && _door.CanOpen(other, door, operatorUid))
            collisionClass = VehicleCollisionClass.Ignore;

        var collisionAabb = GetCollisionAabb(collisionClass, vehicleAabb, movementAabb);
        if (!HasCollisionOverlap(collisionAabb, otherAabb))
            return false;

        candidate = new CollisionCandidate(
            other,
            otherAabb,
            collisionAabb,
            collisionClass,
            door,
            mob,
            isBarricade,
            isXeno,
            isVehicle,
            isUnpoweredDoor);

        return true;
    }

    private CollisionHandlingResult HandleSoftXenoCollision(
        EntityUid vehicle,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 vehicleWorldPosition,
        MapId mapId,
        EntityUid xeno,
        Box2 vehicleAabb,
        Box2 xenoAabb,
        Box2 collisionAabb,
        float clearance,
        bool applyEffects,
        bool debug,
        HashSet<EntityUid>? blockers,
        float wheelDamage,
        ref bool playedCollisionSound)
    {
        if (ShouldBlockXeno(mover, xeno))
        {
            if (applyEffects)
            {
                PlayMobCollisionSound(vehicle, ref playedCollisionSound);
                ApplyWheelCollisionDamage(vehicle, mover, wheelDamage);
            }

            AddBlockingCollision(vehicle, xeno, collisionAabb, xenoAabb, clearance, mapId, debug, blockers);
            return CollisionHandlingResult.Blocked;
        }

        if (!applyEffects)
            return CollisionHandlingResult.Continue;

        PlayMobCollisionSound(vehicle, ref playedCollisionSound);
        var vehicleMove = GetVehicleMoveDelta(grid, vehicleWorldPosition, mapId, mover);
        if (PushMobOutOfVehicle(vehicle, xeno, vehicleAabb, xenoAabb, vehicleMove))
            return CollisionHandlingResult.Continue;

        ApplyWheelCollisionDamage(vehicle, mover, wheelDamage);
        AddBlockingCollision(vehicle, xeno, collisionAabb, xenoAabb, clearance, mapId, debug, blockers);
        return CollisionHandlingResult.Blocked;
    }

    private CollisionHandlingResult HandleBreakableCollision(
        EntityUid vehicle,
        GridVehicleMoverComponent mover,
        EntityUid other,
        Box2 collisionAabb,
        Box2 otherAabb,
        float clearance,
        MapId mapId,
        bool hasDoor,
        bool isUnpoweredDoor,
        bool applyEffects,
        bool debug,
        HashSet<EntityUid>? blockers,
        float wheelDamage,
        ref bool playedCollisionSound)
    {
        if (TryComp(other, out VehicleSmashableComponent? smashable) &&
            smashable.RequiresDoorUnpowered &&
            hasDoor &&
            !isUnpoweredDoor)
        {
            if (applyEffects)
            {
                PlayCollisionSound(vehicle, ref playedCollisionSound);
                ApplyWheelCollisionDamage(vehicle, mover, wheelDamage);
            }

            AddBlockingCollision(vehicle, other, collisionAabb, otherAabb, clearance, mapId, debug, blockers);
            return CollisionHandlingResult.Blocked;
        }

        if (applyEffects)
            TrySmash(other, vehicle, ref playedCollisionSound);

        return CollisionHandlingResult.Continue;
    }

    private CollisionHandlingResult HandleHardCollision(
        EntityUid vehicle,
        GridVehicleMoverComponent mover,
        EntityUid grid,
        Vector2 gridPos,
        EntityUid other,
        Box2 collisionAabb,
        Box2 otherAabb,
        float clearance,
        MapId mapId,
        bool isVehicle,
        bool applyEffects,
        bool debug,
        HashSet<EntityUid>? blockers,
        float wheelDamage,
        ref bool playedCollisionSound)
    {
        if (isVehicle && TryPushVehicle(vehicle, mover, grid, gridPos, other, applyEffects))
            return CollisionHandlingResult.Continue;

        if (applyEffects)
        {
            PlayCollisionSound(vehicle, ref playedCollisionSound);
            ApplyWheelCollisionDamage(vehicle, mover, wheelDamage);
        }

        AddBlockingCollision(vehicle, other, collisionAabb, otherAabb, clearance, mapId, debug, blockers);
        return CollisionHandlingResult.Blocked;
    }

    private static void AddBlockingCollision(
        EntityUid vehicle,
        EntityUid blocker,
        Box2 collisionAabb,
        Box2 blockerAabb,
        float clearance,
        MapId mapId,
        bool debug,
        HashSet<EntityUid>? blockers)
    {
        blockers?.Add(blocker);
        if (debug)
            DebugCollisions.Add(new DebugCollision(vehicle, blocker, collisionAabb, blockerAabb, 0f, 0f, clearance, mapId));
    }

    private static void AddDebugCollisionProbe(
        EntityUid uid,
        GridVehicleMoverComponent mover,
        FixturesComponent fixtures,
        Transform transformData,
        Box2 aabb,
        Box2 movementAabb,
        MapId map,
        bool blocked,
        bool applyEffects)
    {
        if (!TryGetFixtureLocalAabb(fixtures, out var localAabb))
            return;

        var localMovementAabb = GetMovementAabb(localAabb, mover);
        var rotation = new Angle(transformData.Quaternion2D.Angle);
        var fixtureBounds = new Box2Rotated(localAabb.Translated(transformData.Position), rotation, transformData.Position);
        var movementBounds = new Box2Rotated(localMovementAabb.Translated(transformData.Position), rotation, transformData.Position);

        DebugCollisionProbes.Add(new DebugCollisionProbe(
            uid,
            aabb,
            movementAabb,
            fixtureBounds,
            movementBounds,
            transformData.Position,
            rotation,
            blocked,
            applyEffects,
            map));
    }

    private static Box2 GetCollisionAabb(VehicleCollisionClass collisionClass, Box2 fullAabb, Box2 movementAabb)
    {
        return collisionClass == VehicleCollisionClass.SoftMob
            ? fullAabb
            : movementAabb;
    }

    private static bool HasCollisionOverlap(Box2 vehicleAabb, Box2 otherAabb)
    {
        var intersection = vehicleAabb.Intersect(otherAabb);
        return intersection.Width > 0f && intersection.Height > 0f;
    }

    private static Box2 GetMovementAabb(Box2 aabb, GridVehicleMoverComponent mover)
    {
        var inset = Math.Clamp(mover.MovementCollisionInset, 0f, 0.45f);
        if (inset <= 0f)
            return aabb;

        var adjusted = aabb.Enlarged(-inset);
        return adjusted.IsValid() ? adjusted : aabb;
    }

    private Angle GetCollisionWorldRotation(EntityUid uid, EntityUid grid, Angle? overrideRotation)
    {
        if (overrideRotation is not { } localRotation)
            return transform.GetWorldRotation(uid);

        var xform = Transform(uid);
        if (xform.ParentUid.IsValid())
            return transform.GetWorldRotation(xform.ParentUid) + localRotation;

        return transform.GetWorldRotation(grid) + localRotation;
    }

    private bool TryGetDoorPowered(EntityUid target, out bool powered)
    {
        if (TryComp(target, out AirlockComponent? airlock))
        {
            powered = airlock.Powered;
            return true;
        }

        if (TryComp(target, out FirelockComponent? firelock))
        {
            powered = firelock.Powered;
            return true;
        }

        if (HasComp<RMCPowerReceiverComponent>(target))
        {
            powered = _rmcPower.IsPowered(target);
            return true;
        }

        powered = false;
        return false;
    }

    private void ApplyWheelCollisionDamage(EntityUid vehicle, GridVehicleMoverComponent mover, float damage)
    {
        if (_net.IsClient || damage <= 0f)
            return;

        _wheels.DamageWheels(vehicle, damage);
    }

    private float GetWheelCollisionDamage(EntityUid vehicle, GridVehicleMoverComponent mover)
    {
        if (!TryComp(vehicle, out VehicleWheelSlotsComponent? wheels))
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

    private bool HasBlockingVehicleMob(GridVehicleMoverComponent mover, HashSet<EntityUid> blockers)
    {
        foreach (var blocker in blockers)
        {
            if (IsBlockingVehicleMob(mover, blocker))
                return true;
        }

        return false;
    }

    private bool IsBlockingVehicleMob(GridVehicleMoverComponent mover, EntityUid blocker)
    {
        return HasComp<XenoComponent>(blocker) && ShouldBlockXeno(mover, blocker);
    }

    private static bool TryGetFixtureAabb(FixturesComponent fixtures, Transform transformData, out Box2 aabb)
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

    private static bool TryGetFixtureLocalAabb(FixturesComponent fixtures, out Box2 aabb)
    {
        return TryGetFixtureAabb(fixtures, Robust.Shared.Physics.Transform.Empty, out aabb);
    }

    private bool TryPushVehicle(
        EntityUid pusher,
        GridVehicleMoverComponent pusherMover,
        EntityUid grid,
        Vector2 pusherTargetPosition,
        EntityUid pushed,
        bool applyEffects)
    {
        if (!pusherMover.CanPushVehicles)
            return false;

        if (!TryComp(pushed, out VehicleComponent? pushedVehicle) ||
            pushedVehicle.MovementKind != VehicleMovementKind.Grid)
        {
            return false;
        }

        if (!TryComp(pushed, out GridVehicleMoverComponent? pushedMover))
            return false;

        if (!gridQ.TryComp(grid, out var gridComp))
            return false;

        var pushedXform = Transform(pushed);
        if (pushedXform.GridUid != grid)
            return false;

        var pushDelta = pusherTargetPosition - pusherMover.Position;
        if (pushDelta.LengthSquared() <= MinMoveDistance * MinMoveDistance)
            return false;

        TrySyncMoverToCurrentGrid((pushed, pushedMover), centerOnTile: false, pushedXform);
        if (pushedMover.SyncedGrid != grid)
            return false;

        var ignored = new HashSet<EntityUid> { pusher };
        var pushedTarget = pushedMover.Position + pushDelta;
        if (!CanOccupyTransform(
                pushed,
                pushedMover,
                grid,
                pushedTarget,
                null,
                Clearance,
                applyEffects: false,
                debug: false,
                ignoredEntities: ignored))
        {
            return false;
        }

        if (!applyEffects)
            return true;

        if (!CanOccupyTransform(
                pushed,
                pushedMover,
                grid,
                pushedTarget,
                null,
                Clearance,
                applyEffects: true,
                debug: false,
                ignoredEntities: ignored))
        {
            return false;
        }

        pushedMover.Position = pushedTarget;
        pushedMover.CurrentSpeed = 0f;
        pushedMover.IsCommittedToMove = false;
        pushedMover.IsPushMove = true;
        pushedMover.PushDirection = GetCardinalDirection(pushDelta);
        pushedMover.IsMoving = true;
        UpdateDerivedTileState(grid, gridComp, pushedMover);
        SetGridPosition(pushed, grid, pushedMover.Position);
        physics.WakeBody(pushed);
        Dirty(pushed, pushedMover);
        return true;
    }

    private static Vector2i GetCardinalDirection(Vector2 direction)
    {
        if (direction.LengthSquared() <= 0f)
            return Vector2i.Zero;

        if (MathF.Abs(direction.X) >= MathF.Abs(direction.Y))
            return new Vector2i(Math.Sign(direction.X), 0);

        return new Vector2i(0, Math.Sign(direction.Y));
    }

    private bool TrySmash(EntityUid target, EntityUid vehicle, ref bool playedCollisionSound)
    {
        if (!TryComp(target, out VehicleSmashableComponent? smashable))
            return false;

        PlayCollisionSound(vehicle, ref playedCollisionSound);

        if (TryComp(vehicle, out GridVehicleMoverComponent? mover))
            ApplySmashSlowdown(vehicle, mover, smashable);

        if (_net.IsClient)
            return true;

        if (smashable.SmashSound != null)
            _audio.PlayPvs(smashable.SmashSound, Transform(target).Coordinates);

        SmashTarget(target, vehicle, smashable);

        return true;
    }

    private void SmashTarget(EntityUid target, EntityUid vehicle, VehicleSmashableComponent smashable)
    {
        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                [CollisionDamageType] = smashable.DamageOnHit,
            },
        };

        _damageable.TryChangeDamage(target, damage, true, origin: vehicle, tool: vehicle);

        if (!smashable.DeleteOnHit)
            return;

        if (TerminatingOrDeleted(target))
            return;

        _destructible.DestroyEntity(target);
    }

    private void PlayCollisionSound(EntityUid uid, ref bool played)
    {
        if (played)
            return;

        if (!TryComp<VehicleSoundComponent>(uid, out var sound))
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

        if (!TryComp<VehicleSoundComponent>(uid, out var sound))
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
        var runover = EnsureComp<VehicleRunoverComponent>(target);
        runover.Vehicle = vehicle;
        runover.Duration = MobCollisionKnockdown;
        runover.ExpiresAt = now + runover.Duration + VehicleRunoverSystem.StandUpGrace;
        Dirty(target, runover);

        if (physicsQ.TryComp(target, out var targetBody))
        {
            physics.SetLinearVelocity(target, Vector2.Zero, body: targetBody);
            physics.SetAngularVelocity(target, 0f, body: targetBody);
        }
    }

    private Vector2 GetVehicleMoveDelta(
        EntityUid grid,
        Vector2 worldPos,
        MapId mapId,
        GridVehicleMoverComponent mover)
    {
        var currentCoords = new EntityCoordinates(grid, mover.Position);
        var currentWorld = currentCoords.ToMap(EntityManager, transform);
        if (currentWorld.MapId != mapId)
            return Vector2.Zero;

        return worldPos - currentWorld.Position;
    }

    private bool PushMobOutOfVehicle(EntityUid vehicle, EntityUid mob, Box2 vehicleAabb, Box2 mobAabb, Vector2 vehicleMove)
    {
        var xform = Transform(mob);
        if (xform.Anchored)
            return false;

        var centeredAabb = GetCenteredMobAabb(mob, mobAabb);
        if (!TryGetMobPush(vehicle, mob, vehicleAabb, centeredAabb, vehicleMove, out var target))
            return false;

        if (!_net.IsClient || ShouldPredictVehicleInteractions(vehicle))
            ApplyMobPush(mob, target);

        return true;
    }

    private Box2 GetCenteredMobAabb(EntityUid mob, Box2 mobAabb)
    {
        var mobPos = transform.GetWorldPosition(mob);
        var delta = mobAabb.Center - mobPos;
        if (delta.LengthSquared() <= 0.0001f)
            return mobAabb;

        return Box2.CenteredAround(mobPos, mobAabb.Size);
    }

    private void ApplyMobPush(EntityUid mob, EntityCoordinates target)
    {
        if (target == EntityCoordinates.Invalid)
            return;

        var mobMap = transform.GetMapCoordinates(mob);
        var targetMap = transform.ToMapCoordinates(target);
        if (mobMap.MapId != targetMap.MapId)
            return;

        if (physicsQ.TryComp(mob, out var mobBody))
        {
            physics.SetLinearVelocity(mob, Vector2.Zero, body: mobBody);
            physics.SetAngularVelocity(mob, 0f, body: mobBody);
        }

        var mobXform = Transform(mob);
        transform.SetCoordinates(mob, mobXform, target);
    }

    private bool ShouldPredictVehicleInteractions(EntityUid vehicle)
    {
        if (!_net.IsClient || !_timing.InPrediction)
            return false;

        if (!physicsQ.TryComp(vehicle, out var vehicleBody) || !vehicleBody.Predict)
            return false;

        if (!TryComp(vehicle, out VehicleComponent? vehicleComp))
            return false;

        return vehicleComp.Operator != null && vehicleComp.Operator == _player.LocalEntity;
    }

    private void PredictRunover(EntityUid vehicle, EntityUid mob, MobStateComponent mobState)
    {
        if (!ShouldPredictVehicleInteractions(vehicle))
            return;

        if (_mobState.IsDead(mob, mobState) || _standing.IsDown(mob))
            return;

        _stun.TryKnockdown(mob, MobCollisionKnockdown, true);

        var runover = EnsureComp<VehicleRunoverComponent>(mob);
        runover.Vehicle = vehicle;
        runover.Duration = MobCollisionKnockdown;
        runover.ExpiresAt = _timing.CurTime + runover.Duration + VehicleRunoverSystem.StandUpGrace;
        Dirty(mob, runover);

        if (physicsQ.TryComp(mob, out var mobBody))
        {
            physics.SetLinearVelocity(mob, Vector2.Zero, body: mobBody);
            physics.SetAngularVelocity(mob, 0f, body: mobBody);
        }
    }

    private bool TryGetMobPush(
        EntityUid vehicle,
        EntityUid mob,
        Box2 vehicleAabb,
        Box2 mobAabb,
        Vector2 vehicleMove,
        out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;

        var vehicleHalf = vehicleAabb.Size / 2f;
        var mobHalf = mobAabb.Size / 2f;

        var vehicleCenter = vehicleAabb.Center;
        var mobCenter = mobAabb.Center;

        var diff = mobCenter - vehicleCenter;
        var overlapX = vehicleHalf.X + mobHalf.X - Math.Abs(diff.X);
        var overlapY = vehicleHalf.Y + mobHalf.Y - Math.Abs(diff.Y);

        if (overlapX <= 0f || overlapY <= 0f)
            return false;

        if (overlapX <= PushOverlapEpsilon && overlapY <= PushOverlapEpsilon)
            return false;

        var pushX = overlapX > 0f
            ? new Vector2(Math.Sign(diff.X == 0f ? 1f : diff.X) * overlapX, 0f)
            : Vector2.Zero;
        var pushY = overlapY > 0f
            ? new Vector2(0f, Math.Sign(diff.Y == 0f ? 1f : diff.Y) * overlapY)
            : Vector2.Zero;

        var vehicleBounds = vehicleAabb;
        if (TryGetMovementSidePushTarget(
                vehicle,
                mob,
                mobAabb,
                vehicleBounds,
                vehicleMove,
                pushX,
                pushY,
                out target))
        {
            return true;
        }

        if (vehicleMove.LengthSquared() > 0.0001f)
            return false;

        var useX = overlapX < overlapY;
        if (MathF.Abs(overlapX - overlapY) <= PushAxisHysteresis &&
            _lastMobPushAxis.TryGetValue(mob, out var lastUseX))
        {
            useX = lastUseX;
        }

        var first = useX ? pushX : pushY;
        var second = useX ? pushY : pushX;

        if (TryGetSidePushTarget(vehicle, mob, mobAabb, vehicleBounds, first, out target))
        {
            _lastMobPushAxis[mob] = useX;
            return true;
        }

        if (TryGetSidePushTarget(vehicle, mob, mobAabb, vehicleBounds, second, out target))
        {
            _lastMobPushAxis[mob] = !useX;
            return true;
        }

        return false;
    }

    private bool TryGetMovementSidePushTarget(
        EntityUid vehicle,
        EntityUid mob,
        Box2 mobAabb,
        Box2 vehicleBounds,
        Vector2 vehicleMove,
        Vector2 pushX,
        Vector2 pushY,
        out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;

        if (vehicleMove.LengthSquared() <= 0.0001f)
            return false;

        var vehicleMovesX = MathF.Abs(vehicleMove.X) >= MathF.Abs(vehicleMove.Y);
        var sidePush = vehicleMovesX ? pushY : pushX;
        if (sidePush == Vector2.Zero)
            return false;

        var useX = !vehicleMovesX;
        if (TryGetSidePushTarget(vehicle, mob, mobAabb, vehicleBounds, sidePush, out target))
        {
            _lastMobPushAxis[mob] = useX;
            return true;
        }

        if (TryGetSidePushTarget(vehicle, mob, mobAabb, vehicleBounds, -sidePush, out target))
        {
            _lastMobPushAxis[mob] = useX;
            return true;
        }

        return false;
    }

    private bool TryGetSidePushTarget(
        EntityUid vehicle,
        EntityUid mob,
        Box2 mobAabb,
        Box2 vehicleBounds,
        Vector2 push,
        out EntityCoordinates target)
    {
        target = EntityCoordinates.Invalid;
        if (push == Vector2.Zero)
            return false;

        var adjusted = push;
        if (Math.Abs(adjusted.X) > 0f)
            adjusted.X += Math.Sign(adjusted.X) * Clearance;
        if (Math.Abs(adjusted.Y) > 0f)
            adjusted.Y += Math.Sign(adjusted.Y) * Clearance;

        var targetAabb = mobAabb.Translated(adjusted);
        if (targetAabb.Intersects(vehicleBounds))
            return false;

        if (IsPushBlocked(vehicle, mob, mobAabb, adjusted))
            return false;

        var mobMap = transform.GetMapCoordinates(mob);
        var mapCoords = new MapCoordinates(mobMap.Position + adjusted, mobMap.MapId);
        var mobXform = Transform(mob);
        if (mobXform.GridUid is { } grid && gridQ.TryComp(grid, out var gridComp))
        {
            var coords = transform.ToCoordinates(grid, mapCoords);
            var indices = map.TileIndicesFor(grid, gridComp, coords);
            if (IsPushTileBlocked(grid, gridComp, indices, vehicle, mob, out _))
                return false;

            target = transform.ToCoordinates(grid, mapCoords);
        }
        else
        {
            target = transform.ToCoordinates(mapCoords);
        }

        if (target == EntityCoordinates.Invalid)
            return false;

        return true;
    }

    private bool IsPushTileBlocked(
        EntityUid gridUid,
        MapGridComponent gridComp,
        Vector2i indices,
        EntityUid vehicle,
        EntityUid mob,
        out EntityUid blocker)
    {
        blocker = EntityUid.Invalid;
        var gridXform = Transform(gridUid);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var (gridPos, gridRot, matrix) = transform.GetWorldPositionRotationMatrix(gridXform, xformQuery);

        var size = gridComp.TileSize;
        var localPos = new Vector2(indices.X * size + (size / 2f), indices.Y * size + (size / 2f));
        var worldPos = Vector2.Transform(localPos, matrix);

        var tileAabb = Box2.UnitCentered.Scale(0.95f * size);
        var worldBox = new Box2Rotated(tileAabb.Translated(worldPos), gridRot, worldPos);
        tileAabb = tileAabb.Translated(localPos);

        var tileArea = tileAabb.Width * tileAabb.Height;
        var minIntersectionArea = tileArea * PushTileBlockFraction;
        foreach (var ent in lookup.GetEntitiesIntersecting(gridUid, worldBox, LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (ent == vehicle || ent == mob)
                continue;

            if (IsDescendantOf(ent, vehicle) || IsDescendantOf(ent, mob))
                continue;

            var entXformComp = Transform(ent);
            if (HasComp<MobStateComponent>(ent) ||
                HasComp<VehicleSmashableComponent>(ent) ||
                HasComp<FoldableComponent>(ent) ||
                TryComp<DoorComponent>(ent, out _) ||
                HasComp<BarricadeComponent>(ent))
            {
                continue;
            }

            if (HasComp<ItemComponent>(ent) && !entXformComp.Anchored)
                continue;

            if (!physicsQ.TryComp(ent, out var otherBody))
                continue;

            var isVehicle = HasComp<VehicleComponent>(ent);
            if (!entXformComp.Anchored && otherBody.BodyType != BodyType.Static && !isVehicle)
                continue;

            if (!fixtureQ.TryComp(ent, out var fixtures))
                continue;

            if (physicsQ.TryComp(mob, out var mobBody) &&
                fixtureQ.TryComp(mob, out var mobFixtures) &&
                !physics.IsHardCollidable((mob, mobFixtures, mobBody), (ent, fixtures, otherBody)))
            {
                continue;
            }

            var (pos, rot) = transform.GetWorldPositionRotation(entXformComp, xformQuery);
            rot -= gridRot;
            pos = (-gridRot).RotateVec(pos - gridPos);
            var entXform = new Transform(pos, (float) rot.Theta);

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard)
                    continue;

                if ((fixture.CollisionLayer & (int) GridVehiclePushHardBlockMask) == 0)
                    continue;

                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var intersection = fixture.Shape.ComputeAABB(entXform, i).Intersect(tileAabb);
                    var intersectionArea = intersection.Width * intersection.Height;
                    if (intersectionArea > minIntersectionArea)
                    {
                        blocker = ent;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsDescendantOf(EntityUid ent, EntityUid root)
    {
        if (ent == root)
            return true;

        var current = ent;
        while (current.IsValid())
        {
            var xform = Transform(current);
            var parent = xform.ParentUid;
            if (!parent.IsValid())
                return false;

            if (parent == root)
                return true;

            if (parent == xform.GridUid || parent == xform.MapUid)
                return false;

            current = parent;
        }

        return false;
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

        var targetAabb = mobAabb.Translated(push);
        var checkAabb = targetAabb.Enlarged(-PushWallSkin);
        if (!checkAabb.IsValid())
            checkAabb = targetAabb;

        var hits = lookup.GetEntitiesIntersecting(mapId, checkAabb, LookupFlags.Dynamic | LookupFlags.Static);
        foreach (var other in hits)
        {
            if (other == mob || other == vehicle)
                continue;

            if (IsDescendantOf(other, vehicle) || IsDescendantOf(other, mob))
                continue;

            if (!physicsQ.TryComp(other, out var otherBody) || !otherBody.CanCollide)
                continue;

            var otherXform = Transform(other);
            if (!fixtureQ.TryComp(other, out var otherFixtures))
                continue;

            if (HasComp<MobStateComponent>(other) ||
                HasComp<VehicleSmashableComponent>(other) ||
                HasComp<FoldableComponent>(other) ||
                TryComp<DoorComponent>(other, out _) ||
                HasComp<BarricadeComponent>(other))
            {
                continue;
            }

            var wallLike = false;
            var overlaps = false;
            var otherTx = physics.GetPhysicsTransform(other, otherXform);
            foreach (var fixture in otherFixtures.Fixtures.Values)
            {
                if (!fixture.Hard)
                    continue;

                if ((fixture.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                {
                    wallLike = true;
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        var otherAabb = fixture.Shape.ComputeAABB(otherTx, i);
                        var intersection = otherAabb.Intersect(checkAabb);
                        if (Box2.Area(intersection) > PushWallOverlapArea)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (overlaps)
                        break;
                }
            }

            if (!wallLike || !overlaps)
                continue;

            if (physics.IsHardCollidable((mob, mobFixtures, mobBody), (other, otherFixtures, otherBody)))
            {
                return true;
            }
        }

        return false;
    }

    private VehicleCollisionClass ClassifyCollisionCandidate(
        EntityUid other,
        TransformComponent otherXform,
        PhysicsComponent otherBody,
        FixturesComponent otherFixtures,
        bool hardCollidable,
        bool isMob,
        bool isBarricade,
        bool isFoldable,
        bool hasDoor,
        bool isXeno,
        bool isVehicle,
        bool isSmashable)
    {
        if (!otherXform.Anchored && HasComp<ItemComponent>(other))
            return VehicleCollisionClass.Ignore;

        if (isMob || isXeno)
            return VehicleCollisionClass.SoftMob;

        if (IsNormallyMobPassable(otherFixtures))
            return VehicleCollisionClass.Ignore;

        var isLooseDynamic =
            !otherXform.Anchored &&
            otherBody.BodyType != BodyType.Static &&
            !isMob &&
            !isBarricade &&
            !isFoldable &&
            !isVehicle &&
            !isSmashable;

        if (isLooseDynamic)
            return VehicleCollisionClass.Ignore;

        if (isSmashable)
            return VehicleCollisionClass.Breakable;

        if (isBarricade && (hasDoor || isFoldable))
            return VehicleCollisionClass.Breakable;

        if (isFoldable && !hardCollidable)
            return VehicleCollisionClass.Ignore;

        return hardCollidable
            ? VehicleCollisionClass.Hard
            : VehicleCollisionClass.Ignore;
    }

    private static bool IsNormallyMobPassable(FixturesComponent fixtures)
    {
        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!IsNormallyMobPassable(fixture))
                return false;
        }

        return true;
    }

    private static bool IsNormallyMobPassable(Fixture fixture)
    {
        const int mobMask = (int) CollisionGroup.MobMask;
        const int mobLayer = (int) CollisionGroup.MobLayer;

        return !fixture.Hard ||
               ((fixture.CollisionMask & mobLayer) == 0 &&
                (fixture.CollisionLayer & mobMask) == 0);
    }
}
