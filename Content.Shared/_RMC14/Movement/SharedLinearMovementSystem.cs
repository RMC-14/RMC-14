using Content.Shared.ActionBlocker;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Gravity;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared._RMC14.Movement;
public abstract partial class SharedLinearMoverController : VirtualController
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedMoverController _moverController = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    protected EntityQuery<RelayInputMoverComponent> RelayQuery;
    protected EntityQuery<LinearInputMoverComponent> MoverQuery;
    protected EntityQuery<NoRotateOnMoveComponent> NoRotateQuery;
    protected EntityQuery<TransformComponent> XformQuery;
    protected EntityQuery<MovementRelayTargetComponent> RelayTargetQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;
    protected EntityQuery<PullableComponent> PullableQuery;
    protected EntityQuery<MapGridComponent> MapGridQuery;
    protected EntityQuery<CanMoveInAirComponent> CanMoveInAirQuery;
    protected EntityQuery<MapComponent> MapQuery;
    protected EntityQuery<MovementSpeedModifierComponent> ModifierQuery;
    protected EntityQuery<MobMoverComponent> MobMoverQuery;

    private bool _relativeMovement;
    private float _minDamping;
    private float _airDamping;
    private float _offGridDamping;

    /// <summary>
    /// Cache the movement to use elsewhere.
    /// </summary>
    public Dictionary<EntityUid, bool> UsedMobMovement = new();

    public override void Initialize()
    {
        UpdatesBefore.Add(typeof(TileFrictionController));
        base.Initialize();

        RelayQuery = GetEntityQuery<RelayInputMoverComponent>();
        MoverQuery = GetEntityQuery<LinearInputMoverComponent>();
        XformQuery = GetEntityQuery<TransformComponent>();
        NoRotateQuery = GetEntityQuery<NoRotateOnMoveComponent>();
        RelayTargetQuery = GetEntityQuery<MovementRelayTargetComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        PullableQuery = GetEntityQuery<PullableComponent>();
        MapGridQuery = GetEntityQuery<MapGridComponent>();
        CanMoveInAirQuery = GetEntityQuery<CanMoveInAirComponent>();
        MapQuery = GetEntityQuery<MapComponent>();
        ModifierQuery = GetEntityQuery<MovementSpeedModifierComponent>();
        MobMoverQuery = GetEntityQuery<MobMoverComponent>();

        InitializeInput();
        InitializeRelay();
        Subs.CVar(_configManager, CCVars.RelativeMovement, value => _relativeMovement = value, true);
        Subs.CVar(_configManager, CCVars.MinFriction, value => _minDamping = value, true);
        Subs.CVar(_configManager, CCVars.AirFriction, value => _airDamping = value, true);
        Subs.CVar(_configManager, CCVars.OffgridFriction, value => _offGridDamping = value, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownInput();
    }

    protected void HandleLinearMovement(
    Entity<LinearInputMoverComponent> entity,
    float frameTime)
    {
        var uid = entity.Owner;
        var mover = entity.Comp;

        // If we're a relay then apply all of our data to the parent instead and go next.
        if (RelayQuery.TryComp(uid, out var relay))
        {
            if (!MoverQuery.TryComp(relay.RelayEntity, out var relayTargetMover))
                return;

            // Always lerp rotation so relay entities aren't cooked.
            LerpRotation(uid, mover, frameTime);
            var dirtied = false;

            if (relayTargetMover.RelativeEntity != mover.RelativeEntity)
            {
                relayTargetMover.RelativeEntity = mover.RelativeEntity;
                dirtied = true;
            }

            if (relayTargetMover.RelativeRotation != mover.RelativeRotation)
            {
                relayTargetMover.RelativeRotation = mover.RelativeRotation;
                dirtied = true;
            }

            if (relayTargetMover.TargetRelativeRotation != mover.TargetRelativeRotation)
            {
                relayTargetMover.TargetRelativeRotation = mover.TargetRelativeRotation;
                dirtied = true;
            }

            if (relayTargetMover.CanMove != mover.CanMove)
            {
                relayTargetMover.CanMove = mover.CanMove;
                dirtied = true;
            }

            if (dirtied)
            {
                Dirty(relay.RelayEntity, relayTargetMover);
            }

            return;
        }

        if (!XformQuery.TryComp(entity.Owner, out var xform))
            return;

        RelayTargetQuery.TryComp(uid, out var relayTarget);
        var relaySource = relayTarget?.Source;

        // If we're not the target of a relay then handle lerp data.
        if (relaySource == null)
        {
            // Update relative movement
            if (mover.LerpTarget < Timing.CurTime)
            {
                TryUpdateRelative(uid, mover, xform);
            }

            LerpRotation(uid, mover, frameTime);
        }

        // If we can't move then just use tile-friction / no movement handling.
        if (!mover.CanMove
            || !PhysicsQuery.TryComp(uid, out var physicsComponent)
            || PullableQuery.TryGetComponent(uid, out var pullable) && pullable.BeingPulled)
        {
            UsedMobMovement[uid] = false;
            return;
        }

        // We don't use this in RMC but who knows, someone may like this.
        var weightless = _gravity.IsWeightless(uid, physicsComponent, xform);
        var inAirHelpless = false;

        if (physicsComponent.BodyStatus != BodyStatus.OnGround && !CanMoveInAirQuery.HasComponent(uid))
        {
            if (!weightless)
            {
                UsedMobMovement[uid] = false;
                return;
            }
            inAirHelpless = true;
        }

        UsedMobMovement[uid] = true;

        var moveSpeedComponent = ModifierQuery.CompOrNull(uid);

        float friction;
        float accel;
        Vector2 wishDir;
        var velocity = physicsComponent.LinearVelocity;
        var angVelocity = physicsComponent.AngularVelocity;

        // Get current tile def for things like speed/friction mods
        ContentTileDefinition? tileDef = null;

        var touching = false;
        // Whether we use tilefriction or not || RMC doesn't use the gravity system but maybe someone likes this.
        if (weightless || inAirHelpless)
        {
            // Find the speed we should be moving at and make sure we're not trying to move faster than that
            var walkSpeed = moveSpeedComponent?.WeightlessWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            var sprintSpeed = moveSpeedComponent?.WeightlessSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

            wishDir = AssertValidLinearWish(mover, walkSpeed, sprintSpeed);

            var ev = new CanWeightlessMoveEvent(uid);
            RaiseLocalEvent(uid, ref ev, true);

            touching = ev.CanMove || xform.GridUid != null || MapGridQuery.HasComp(xform.GridUid);

            // If we're not on a grid, and not able to move in space check if we're close enough to a grid to touch.
            if (!touching && MobMoverQuery.TryComp(uid, out var mobMover))
                touching |= IsAroundCollider(_lookup, (uid, physicsComponent, mobMover, xform));

            // If we're touching then use the weightless values
            if (touching)
            {
                touching = true;
                if (wishDir != Vector2.Zero)
                    friction = moveSpeedComponent?.WeightlessFriction ?? _airDamping;
                else
                    friction = moveSpeedComponent?.WeightlessFrictionNoInput ?? _airDamping;
            }
            // Otherwise use the off-grid values.
            else
            {
                friction = moveSpeedComponent?.OffGridFriction ?? _offGridDamping;
            }

            accel = moveSpeedComponent?.WeightlessAcceleration ?? MovementSpeedModifierComponent.DefaultWeightlessAcceleration;
        }
        else
        {
            if (MapGridQuery.TryComp(xform.GridUid, out var gridComp)
                && _mapSystem.TryGetTileRef(xform.GridUid.Value, gridComp, xform.Coordinates, out var tile)
                && physicsComponent.BodyStatus == BodyStatus.OnGround)
                tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            var walkSpeed = moveSpeedComponent?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            var sprintSpeed = moveSpeedComponent?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

            wishDir = AssertValidLinearWish(mover, walkSpeed, sprintSpeed);

            if (wishDir != Vector2.Zero)
            {
                friction = moveSpeedComponent?.Friction ?? MovementSpeedModifierComponent.DefaultFriction;
                friction *= tileDef?.MobFriction ?? tileDef?.Friction ?? 1f;
            }
            else
            {
                friction = moveSpeedComponent?.FrictionNoInput ?? MovementSpeedModifierComponent.DefaultFrictionNoInput;
                friction *= tileDef?.Friction ?? 1f;
            }

            accel = moveSpeedComponent?.Acceleration ?? MovementSpeedModifierComponent.DefaultAcceleration;
            accel *= tileDef?.MobAcceleration ?? 1f;
        }

        // This way friction never exceeds acceleration when you're trying to move.
        // If you want to slow down an entity with "friction" you shouldn't be using this system.
        if (wishDir != Vector2.Zero)
            friction = Math.Min(friction, accel);
        friction = Math.Max(friction, _minDamping);
        var minimumFrictionSpeed = moveSpeedComponent?.MinimumFrictionSpeed ?? MovementSpeedModifierComponent.DefaultMinimumFrictionSpeed;
        _moverController.Friction(minimumFrictionSpeed, frameTime, friction, ref velocity);

        if (!weightless || touching)
            Accelerate(ref velocity, in wishDir, accel, frameTime);

        SetWishDir((uid, mover), wishDir);

        PhysicsSystem.SetLinearVelocity(uid, velocity, body: physicsComponent);

        // Ensures that players do not spiiiiiiin
        PhysicsSystem.SetAngularVelocity(uid, angVelocity, body: physicsComponent);

        // Handle footsteps at the end
        if (wishDir != Vector2.Zero)
        {
            // We probably don't want to lock rotation for linear movement as it requires rotation to move.
            if (!NoRotateQuery.HasComponent(uid))
            {
                // TODO apparently this results in a duplicate move event because "This should have its event run during
                // island solver"??. So maybe SetRotation needs an argument to avoid raising an event?
                var worldRot = _transform.GetWorldRotation(xform);

                _transform.SetLocalRotation(uid, xform.LocalRotation + wishDir.ToWorldAngle() - worldRot, xform);
            }
            // TODO RMC: Fix the sounds.
            /*
            if (!weightless && MobMoverQuery.TryGetComponent(uid, out var mobMover) &&
                TryGetSound(weightless, uid, mover, mobMover, xform, out var sound, tileDef: tileDef))
            {
                var soundModifier = mover.Sprinting ? 3.5f : 1.5f;

                var audioParams = sound.Params
                    .WithVolume(sound.Params.Volume + soundModifier)
                    .WithVariation(sound.Params.Variation ?? mobMover.FootstepVariation);

                // If we're a relay target then predict the sound for all relays.
                if (relaySource != null)
                {
                    _audio.PlayPredicted(sound, uid, relaySource.Value, audioParams);
                }
                else
                {
                    _audio.PlayPredicted(sound, uid, uid, audioParams);
                }
            }*/
        }
    }

    // We don't need this for RMC but it is here for portability. If anyone is crazy enough to actually use this code.
    /// <summary>
    /// Used for weightlessness to determine if we are near a wall.
    /// </summary>
    private bool IsAroundCollider(EntityLookupSystem lookupSystem, Entity<PhysicsComponent, MobMoverComponent, TransformComponent> entity)
    {
        var (uid, collider, mover, transform) = entity;
        var enlargedAABB = _lookup.GetWorldAABB(entity.Owner, transform).Enlarged(mover.GrabRange);

        foreach (var otherEntity in lookupSystem.GetEntitiesIntersecting(transform.MapID, enlargedAABB))
        {
            if (otherEntity == uid)
                continue; // Don't try to push off of yourself!

            if (!PhysicsQuery.TryComp(otherEntity, out var otherCollider))
                continue;

            // Only allow pushing off of anchored things that have collision.
            if (otherCollider.BodyType != BodyType.Static ||
                !otherCollider.CanCollide ||
                ((collider.CollisionMask & otherCollider.CollisionLayer) == 0 &&
                (otherCollider.CollisionMask & collider.CollisionLayer) == 0) ||
                (TryComp(otherEntity, out PullableComponent? pullable) && pullable.BeingPulled))
            {
                continue;
            }

            return true;
        }

        return false;
    }


    /// <summary>
    /// Adjusts the current velocity to the target velocity based on the specified acceleration.
    /// </summary>
    public static void Accelerate(ref Vector2 currentVelocity, in Vector2 velocity, float accel, float frameTime)
    {
        var wishDir = velocity != Vector2.Zero ? velocity.Normalized() : Vector2.Zero;
        var wishSpeed = velocity.Length();

        var currentSpeed = Vector2.Dot(currentVelocity, wishDir);
        var addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0f)
            return;

        var accelSpeed = accel * frameTime * wishSpeed;
        accelSpeed = MathF.Min(accelSpeed, addSpeed);

        currentVelocity += wishDir * accelSpeed;
    }

    public void LerpRotation(EntityUid uid, LinearInputMoverComponent mover, float frameTime)
    {
        var angleDiff = Angle.ShortestDistance(mover.RelativeRotation, mover.TargetRelativeRotation);

        // if we've just traversed then lerp to our target rotation.
        if (!angleDiff.EqualsApprox(Angle.Zero, 0.001))
        {
            var adjustment = angleDiff * 5f * frameTime;
            var minAdjustment = 0.01 * frameTime;

            if (angleDiff < 0)
            {
                adjustment = Math.Min(adjustment, -minAdjustment);
                adjustment = Math.Clamp(adjustment, angleDiff, -angleDiff);
            }
            else
            {
                adjustment = Math.Max(adjustment, minAdjustment);
                adjustment = Math.Clamp(adjustment, -angleDiff, angleDiff);
            }

            mover.RelativeRotation = (mover.RelativeRotation + adjustment).FlipPositive();
            Dirty(uid, mover);
        }
        else if (!angleDiff.Equals(Angle.Zero))
        {
            mover.RelativeRotation = mover.TargetRelativeRotation.FlipPositive();
            Dirty(uid, mover);
        }
    }

    public Vector2 GetWishDir(Entity<LinearInputMoverComponent?> mover)
    {
        if (!MoverQuery.Resolve(mover.Owner, ref mover.Comp, false))
            return Vector2.Zero;

        return mover.Comp.WishDir;
    }

    public void SetWishDir(Entity<LinearInputMoverComponent> mover, Vector2 wishDir)
    {
        if (mover.Comp.WishDir.Equals(wishDir))
            return;

        mover.Comp.WishDir = wishDir;
        Dirty(mover);
    }
}
