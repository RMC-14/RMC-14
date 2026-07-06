using System;
using System.Numerics;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleTurretSystem : EntitySystem
{
    private static readonly EntProtoId TurretVisual = "VehicleTurretVisual";

    private const float PixelsPerMeter = 32f;
    private const float FireAlignmentToleranceDegrees = 2f;

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(GridVehicleMoverSystem));
        SubscribeLocalEvent<VehicleTurretComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<VehicleTurretComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<VehicleTurretComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VehicleTurretComponent, AttemptShootEvent>(OnAttemptShoot, after: new[] { typeof(GunMuzzleOffsetSystem), typeof(VehicleTurretMuzzleSystem) });
        SubscribeNetworkEvent<VehicleTurretRotateEvent>(OnRotateEvent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient && _timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<VehicleTurretComponent>();
        while (query.MoveNext(out var uid, out var turret))
        {
            if (!ShouldUpdateTransforms(turret))
                continue;

            if (!TryGetVehicle(uid, out var vehicle))
            {
                CleanupVisual(turret);
                continue;
            }

            UpdateTurretRotation(uid, turret, vehicle, frameTime);
        }

        query = EntityQueryEnumerator<VehicleTurretComponent>();
        while (query.MoveNext(out var uid, out var turret))
        {
            if (!ShouldUpdateTransforms(turret))
                continue;

            if (!TryGetVehicle(uid, out var vehicle))
            {
                CleanupVisual(turret);
                continue;
            }

            TryGetAnchorTurret(uid, turret, out var anchorUid, out var anchorTurret);

            EnsureVisual(uid, turret, vehicle);
            InitializeRotation(anchorUid, anchorTurret, vehicle);
            UpdateTurretTransforms(uid, turret, vehicle, anchorUid, anchorTurret);
        }
    }

    private void OnInserted(Entity<VehicleTurretComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_net.IsClient && _timing.ApplyingState)
            return;

        if (!ShouldUpdateTransforms(ent.Comp))
            return;

        if (!TryGetVehicle(ent.Owner, out var vehicle))
            return;

        UpdateTurretRotation(ent.Owner, ent.Comp, vehicle, 0f);
        TryGetAnchorTurret(ent.Owner, ent.Comp, out var anchorUid, out var anchorTurret);

        EnsureVisual(ent.Owner, ent.Comp, vehicle);
        InitializeRotation(anchorUid, anchorTurret, vehicle);
        UpdateTurretTransforms(ent.Owner, ent.Comp, vehicle, anchorUid, anchorTurret);
    }

    private void OnRemoved(Entity<VehicleTurretComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        CleanupVisual(ent.Comp);
    }

    private void OnShutdown(Entity<VehicleTurretComponent> ent, ref ComponentShutdown args)
    {
        CleanupVisual(ent.Comp);
    }

    private void OnRotateEvent(VehicleTurretRotateEvent args, EntitySessionEventArgs session)
    {
        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return;

        if (_net.IsClient && _timing.ApplyingState)
            return;

        var turretUid = GetEntity(args.Turret);
        if (!TryComp(turretUid, out VehicleTurretComponent? turret))
            return;

        if (!TryGetVehicle(turretUid, out var vehicle))
            return;

        if (!_net.IsClient)
        {
            if (session.SenderSession.AttachedEntity is not { } user)
                return;

            if (TryComp(user, out VehicleViewToggleComponent? viewToggle) && !viewToggle.IsOutside)
                return;

            if (!TryComp(user, out VehicleWeaponsOperatorComponent? operatorComp) ||
                operatorComp.Vehicle != vehicle ||
                operatorComp.SelectedWeapon != turretUid)
            {
                return;
            }
        }

        if (!TryResolveRotationTarget(turretUid, turret, out var targetUid, out var targetTurret))
            return;

        if (!targetTurret.RotateToCursor)
            return;

        if (!TryGetTurretOrigin(targetUid, out var originCoords))
            return;

        var targetCoords = GetCoordinates(args.Coordinates);
        var originMap = _transform.ToMapCoordinates(originCoords);
        var targetMap = _transform.ToMapCoordinates(targetCoords);
        var direction = targetMap.Position - originMap.Position;

        if (direction.LengthSquared() <= 0.0001f)
            return;

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var desiredRotation = targetTurret.StabilizedRotation
            ? direction.ToWorldAngle()
            : (direction.ToWorldAngle() - vehicleRot).Reduced();

        SetTargetRotation(targetUid, targetTurret, vehicle, desiredRotation, allowReverseDelay: true);
    }

    private void EnsureVisual(EntityUid turretUid, VehicleTurretComponent turret, EntityUid vehicle)
    {
        if (_net.IsClient || !turret.ShowOverlay)
            return;

        if (turret.VisualEntity is { } existing && Exists(existing))
            return;

        var visual = Spawn(TurretVisual, new EntityCoordinates(vehicle, Vector2.Zero));
        var visualComp = EnsureComp<VehicleTurretVisualComponent>(visual);
        visualComp.Turret = GetNetEntity(turretUid);
        Dirty(visual, visualComp);
        turret.VisualEntity = visual;
    }

    private void CleanupVisual(VehicleTurretComponent turret)
    {
        if (_net.IsClient)
            return;

        if (turret.VisualEntity is not { } visual)
            return;

        if (Exists(visual) && !TerminatingOrDeleted(visual) && !EntityManager.IsQueuedForDeletion(visual))
            Del(visual);

        turret.VisualEntity = null;
    }

    private void UpdateTurretTransforms(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        EntityUid vehicle,
        EntityUid anchorUid,
        VehicleTurretComponent anchorTurret)
    {
        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var baseFacingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
        var anchorFacingAngle = GetOffsetFacing(anchorTurret, anchorTurret, vehicleRot, baseFacingAngle);
        var anchorLocalOffset = (-vehicleRot).RotateVec(GetPixelOffset(anchorTurret, anchorFacingAngle) / PixelsPerMeter);
        var localRot = Angle.Zero;
        if (anchorTurret.RotateToCursor)
            localRot = anchorTurret.WorldRotation;

        EntityCoordinates turretCoords;
        Angle turretLocalRot;
        EntityCoordinates visualCoords;
        Angle visualLocalRot;

        if (anchorUid == turretUid)
        {
            turretCoords = new EntityCoordinates(vehicle, anchorLocalOffset);
            turretLocalRot = localRot;
            visualCoords = turretCoords;
            visualLocalRot = localRot;
        }
        else
        {
            var turretFacingAngle = GetOffsetFacing(turret, anchorTurret, vehicleRot, baseFacingAngle);
            var worldOffset = GetPixelOffset(turret, turretFacingAngle) / PixelsPerMeter;
            Vector2 relativeAnchorOffset;
            Vector2 turretLocalOffset;

            if (turret.OffsetRotatesWithTurret)
            {
                if (turret.UseDirectionalOffsets)
                {
                    var dir = GetDirectionalDir(turretFacingAngle);
                    var directionalOffset = (turret.PixelOffset + GetDirectionalOffset(turret, dir)) / PixelsPerMeter;
                    var snappedAngle = GetDirectionalAngle(dir);
                    relativeAnchorOffset = (-snappedAngle).RotateVec(directionalOffset);
                    turretLocalOffset = (localRot - snappedAngle).RotateVec(directionalOffset);
                }
                else
                {
                    relativeAnchorOffset = worldOffset;
                    turretLocalOffset = localRot.RotateVec(relativeAnchorOffset);
                }
            }
            else
            {
                turretLocalOffset = (-vehicleRot).RotateVec(worldOffset);
                relativeAnchorOffset = (-localRot).RotateVec(turretLocalOffset);
            }
            turretCoords = new EntityCoordinates(anchorUid, relativeAnchorOffset);
            turretLocalRot = Angle.Zero;
            visualCoords = new EntityCoordinates(vehicle, anchorLocalOffset + turretLocalOffset);
            visualLocalRot = localRot;
        }

        var turretXform = Transform(turretUid);
        _transform.SetCoordinates(turretUid, turretXform, turretCoords);
        _transform.SetLocalRotation(turretUid, turretLocalRot, turretXform);

        if (turret.VisualEntity is not { } visual || !Exists(visual))
            return;

        var visualXform = Transform(visual);
        _transform.SetCoordinates(visual, visualXform, visualCoords);
        _transform.SetLocalRotation(visual, visualLocalRot, visualXform);
    }

    public void TryGetAnchorTurret(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        out EntityUid anchorUid,
        out VehicleTurretComponent anchorTurret)
    {
        anchorUid = turretUid;
        anchorTurret = turret;

        if (!HasComp<VehicleTurretAttachmentComponent>(turretUid))
            return;

        if (!TryGetParentTurret(turretUid, out var parentUid, out var parentTurret))
            return;

        anchorUid = parentUid;
        anchorTurret = parentTurret;
    }

    public bool TryGetTurretOrigin(EntityUid turretUid, out EntityCoordinates origin)
    {
        origin = default;

        return TryGetTurretPose(turretUid, out origin, out _);
    }

    public bool TryGetTurretPose(
        EntityUid turretUid,
        out EntityCoordinates origin,
        out Angle worldRotation,
        VehicleTurretComponent? turret = null)
    {
        origin = default;
        worldRotation = Angle.Zero;

        if (!Resolve(turretUid, ref turret, false) ||
            !TryGetVehicle(turretUid, out var vehicle))
        {
            return false;
        }

        TryGetAnchorTurret(turretUid, turret, out var anchorUid, out var anchorTurret);

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var baseFacingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
        var anchorFacingAngle = GetOffsetFacing(anchorTurret, anchorTurret, vehicleRot, baseFacingAngle);
        var localOffset = (-vehicleRot).RotateVec(GetPixelOffset(anchorTurret, anchorFacingAngle) / PixelsPerMeter);
        var localRot = anchorTurret.RotateToCursor
            ? anchorTurret.WorldRotation
            : Angle.Zero;

        if (anchorUid != turretUid)
        {
            var turretFacingAngle = GetOffsetFacing(turret, anchorTurret, vehicleRot, baseFacingAngle);
            var worldOffset = GetPixelOffset(turret, turretFacingAngle) / PixelsPerMeter;
            Vector2 turretLocalOffset;

            if (turret.OffsetRotatesWithTurret)
            {
                if (turret.UseDirectionalOffsets)
                {
                    var dir = GetDirectionalDir(turretFacingAngle);
                    var directionalOffset = (turret.PixelOffset + GetDirectionalOffset(turret, dir)) / PixelsPerMeter;
                    var snappedAngle = GetDirectionalAngle(dir);
                    turretLocalOffset = (localRot - snappedAngle).RotateVec(directionalOffset);
                }
                else
                {
                    turretLocalOffset = localRot.RotateVec(worldOffset);
                }
            }
            else
            {
                turretLocalOffset = (-vehicleRot).RotateVec(worldOffset);
            }

            localOffset += turretLocalOffset;
        }

        origin = new EntityCoordinates(vehicle, localOffset);
        worldRotation = (vehicleRot + localRot).Reduced();
        return true;
    }

    public Vector2 GetPixelOffset(VehicleTurretComponent turret, Angle facing)
    {
        if (!turret.UseDirectionalOffsets)
            return turret.PixelOffset;

        var baseOffset = turret.PixelOffset;
        var normalized = facing.Theta % MathHelper.TwoPi;
        if (normalized < 0)
            normalized += MathHelper.TwoPi;

        var dir = GetDirectionalDir((float) normalized);
        return baseOffset + GetDirectionalOffset(turret, dir);
    }

    private static Vector2 GetDirectionalOffset(VehicleTurretComponent turret, Direction dir)
    {
        return dir switch
        {
            Direction.South => turret.PixelOffsetSouth,
            Direction.East => turret.PixelOffsetEast,
            Direction.North => turret.PixelOffsetNorth,
            Direction.West => turret.PixelOffsetWest,
            _ => Vector2.Zero
        };
    }

    private static Direction GetDirectionalDir(Angle facing)
    {
        return VehicleTurretDirectionHelpers.GetRenderAlignedCardinalDir(facing);
    }

    private static Direction GetDirectionalDir(float normalized)
    {
        return VehicleTurretDirectionHelpers.GetRenderAlignedCardinalDir(new Angle(normalized));
    }

    private static Angle GetDirectionalAngle(Direction dir)
    {
        return dir.ToAngle();
    }

    public Angle GetVehicleFacingAngle(EntityUid vehicle, Angle vehicleRot)
    {
        if (TryComp(vehicle, out GridVehicleMoverComponent? mover) && mover.CurrentDirection != Vector2i.Zero)
            return new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

        return vehicleRot;
    }

    public Angle GetOffsetFacing(
        VehicleTurretComponent turret,
        VehicleTurretComponent anchorTurret,
        Angle vehicleRot,
        Angle baseFacingAngle)
    {
        if (!turret.OffsetRotatesWithTurret)
            return baseFacingAngle;

        return (vehicleRot + anchorTurret.WorldRotation).Reduced();
    }

    public bool TryGetVehicle(EntityUid turretUid, out EntityUid vehicle)
    {
        vehicle = default;
        var current = turretUid;

        while (_container.TryGetContainingContainer((current, null), out var container))
        {
            var owner = container.Owner;
            if (HasComp<VehicleComponent>(owner))
            {
                vehicle = owner;
                return true;
            }

            current = owner;
        }

        return false;
    }

    public bool TryResolveRotationTarget(
        EntityUid turretUid,
        out EntityUid targetUid,
        out VehicleTurretComponent targetTurret)
    {
        targetUid = default;
        targetTurret = default!;

        if (!TryComp(turretUid, out VehicleTurretComponent? turret))
            return false;

        return TryResolveRotationTarget(turretUid, turret, out targetUid, out targetTurret);
    }

    public bool TryGetBarrelWorldRotation(
        EntityUid turretUid,
        out Angle worldRotation,
        VehicleTurretComponent? turret = null)
    {
        worldRotation = Angle.Zero;

        if (!Resolve(turretUid, ref turret, false))
            return false;

        if (!TryResolveRotationTarget(turretUid, turret, out var targetUid, out var targetTurret))
            return false;

        if (!TryGetVehicle(targetUid, out var vehicle))
            return false;

        worldRotation = (targetTurret.WorldRotation + _transform.GetWorldRotation(vehicle)).Reduced();
        return true;
    }

    public bool TryGetShotBarrelWorldRotation(
        EntityUid turretUid,
        out Angle worldRotation,
        VehicleTurretComponent? turret = null)
    {
        worldRotation = Angle.Zero;

        if (!Resolve(turretUid, ref turret, false) ||
            !turret.UseBarrelDirectionForShots)
        {
            return false;
        }

        return TryGetBarrelWorldRotation(turretUid, out worldRotation, turret);
    }

    private bool TryResolveRotationTarget(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        out EntityUid targetUid,
        out VehicleTurretComponent targetTurret)
    {
        targetUid = turretUid;
        targetTurret = turret;

        if (!HasComp<VehicleTurretAttachmentComponent>(turretUid))
            return true;

        if (!TryGetParentTurret(turretUid, out var parentUid, out var parentTurret))
            return true;

        targetUid = parentUid;
        targetTurret = parentTurret;
        return true;
    }

    public bool TryGetParentTurret(
        EntityUid turretUid,
        out EntityUid parentUid,
        out VehicleTurretComponent parentTurret)
    {
        parentUid = default;
        parentTurret = default!;
        var current = turretUid;

        while (_container.TryGetContainingContainer((current, null), out var container))
        {
            var owner = container.Owner;
            if (TryComp(owner, out VehicleTurretComponent? turret))
            {
                parentUid = owner;
                parentTurret = turret;
                return true;
            }

            current = owner;
        }

        return false;
    }

    public bool TryAimAtTarget(EntityUid turretUid, EntityUid target, out EntityCoordinates targetCoords)
    {
        targetCoords = default;

        if (!TryResolveRotationTarget(turretUid, out var targetUid, out var targetTurret))
            return false;

        if (!targetTurret.RotateToCursor)
            return false;

        if (!TryGetVehicle(targetUid, out var vehicle))
            return false;

        if (!TryGetTurretOrigin(targetUid, out var originCoords))
            return false;

        targetCoords = Transform(target).Coordinates;
        var originMap = _transform.ToMapCoordinates(originCoords);
        var targetMap = _transform.ToMapCoordinates(targetCoords);
        var direction = targetMap.Position - originMap.Position;
        if (direction.LengthSquared() <= 0.0001f)
            return false;

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var desiredRotation = targetTurret.StabilizedRotation
            ? direction.ToWorldAngle()
            : (direction.ToWorldAngle() - vehicleRot).Reduced();

        targetTurret.TargetRotation = desiredRotation;
        if (targetTurret.RotationSpeed <= 0f)
        {
            var desiredLocal = targetTurret.StabilizedRotation
                ? (desiredRotation - vehicleRot).Reduced()
                : desiredRotation;
            targetTurret.WorldRotation = desiredLocal;
        }

        Dirty(targetUid, targetTurret);
        UpdateTurretTransforms(targetUid, targetTurret, vehicle, targetUid, targetTurret);
        return true;
    }

    public bool TrySetTargetRotationWorld(EntityUid turretUid, Angle worldRotation)
    {
        if (!TryResolveRotationTarget(turretUid, out var targetUid, out var targetTurret))
            return false;

        if (!TryGetVehicle(targetUid, out var vehicle))
            return false;

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var desiredRotation = targetTurret.StabilizedRotation
            ? worldRotation
            : (worldRotation - vehicleRot).Reduced();

        targetTurret.TargetRotation = desiredRotation;
        if (targetTurret.RotationSpeed <= 0f)
        {
            var desiredLocal = targetTurret.StabilizedRotation
                ? (desiredRotation - vehicleRot).Reduced()
                : desiredRotation;
            targetTurret.WorldRotation = desiredLocal;
        }

        Dirty(targetUid, targetTurret);
        UpdateTurretTransforms(targetUid, targetTurret, vehicle, targetUid, targetTurret);
        return true;
    }

    private void InitializeRotation(EntityUid turretUid, VehicleTurretComponent turret, EntityUid vehicle)
    {
        if (_net.IsClient)
            return;

        if ((!turret.RotateToCursor && !turret.ShowOverlay) || turret.WorldRotation != Angle.Zero)
        {
            if (turret.TargetRotation == Angle.Zero && turret.WorldRotation != Angle.Zero)
            {
                var vehicleRot = _transform.GetWorldRotation(vehicle);
                turret.TargetRotation = turret.StabilizedRotation
                    ? (turret.WorldRotation + vehicleRot).Reduced()
                    : turret.WorldRotation;
                Dirty(turretUid, turret);
            }
            return;
        }

        var baseWorld = _transform.GetWorldRotation(vehicle);
        turret.WorldRotation = Angle.Zero;
        turret.TargetRotation = turret.StabilizedRotation ? baseWorld : Angle.Zero;
        turret.LastVehicleRotation = baseWorld;
        turret.LastVehicleRotationValid = true;
        Dirty(turretUid, turret);
    }

    private void UpdateTurretRotation(EntityUid turretUid, VehicleTurretComponent turret, EntityUid vehicle, float frameTime)
    {
        var vehicleRot = _transform.GetWorldRotation(vehicle);

        if (turret.StabilizedRotation && turret.RotationSpeed > 0f && (!_net.IsClient || IsLocallyOperatedTurret(turretUid)))
        {
            var dirty = false;

            if (turret.LastVehicleRotationValid)
            {
                var rotDelta = Angle.ShortestDistance(turret.LastVehicleRotation, vehicleRot);
                if (rotDelta.Theta != 0.0)
                {
                    turret.WorldRotation = (turret.WorldRotation - rotDelta).Reduced();
                    dirty = true;
                }
            }

            if (turret.LastVehicleRotation != vehicleRot || !turret.LastVehicleRotationValid)
            {
                turret.LastVehicleRotation = vehicleRot;
                turret.LastVehicleRotationValid = true;
                dirty = true;
            }

            if (dirty)
                Dirty(turretUid, turret);
        }

        if (!turret.RotateToCursor)
            return;

        if (_net.IsClient && !IsLocallyOperatedTurret(turretUid))
            return;

        ApplyPendingTargetRotation(turretUid, turret, vehicle);

        if (turret.TargetRotation == Angle.Zero && turret.WorldRotation != Angle.Zero)
        {
            turret.TargetRotation = turret.StabilizedRotation
                ? (turret.WorldRotation + vehicleRot).Reduced()
                : turret.WorldRotation;
            Dirty(turretUid, turret);
            return;
        }

        var target = turret.StabilizedRotation
            ? (turret.TargetRotation - vehicleRot).Reduced()
            : turret.TargetRotation;

        if (turret.RotationSpeed <= 0f)
        {
            if (turret.WorldRotation != target)
            {
                turret.WorldRotation = target;
                Dirty(turretUid, turret);
            }

            return;
        }

        var delta = Angle.ShortestDistance(turret.WorldRotation, target);
        var maxStep = MathHelper.DegreesToRadians(turret.RotationSpeed) * frameTime;
        if (Math.Abs(delta.Theta) <= maxStep)
        {
            if (turret.WorldRotation != target)
            {
                turret.WorldRotation = target;
                Dirty(turretUid, turret);
            }

            return;
        }

        var step = Math.Sign(delta.Theta) * maxStep;
        var next = (turret.WorldRotation + step).Reduced();
        if (next != turret.WorldRotation)
        {
            turret.WorldRotation = next;
            Dirty(turretUid, turret);
        }
    }

    private void OnAttemptShoot(Entity<VehicleTurretComponent> ent, ref AttemptShootEvent args)
    {
        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
            return;

        if (!IsTurretFunctional(ent.Owner))
        {
            args.Cancelled = true;
            args.ResetCooldown = true;
            return;
        }

        if (!CanOperatorUseTurret(ent.Owner, args.User))
        {
            args.Cancelled = true;
            args.ResetCooldown = true;
            return;
        }

        if (!TryResolveRotationTarget(ent.Owner, ent.Comp, out var targetUid, out var targetTurret))
            return;

        if (!targetTurret.RotateToCursor)
            return;

        var alignmentTolerance = MathHelper.DegreesToRadians(
            MathF.Max(FireAlignmentToleranceDegrees + ent.Comp.FireWhileRotatingGraceDegrees, 0f));

        if (!TryGetVehicle(targetUid, out var vehicle))
            return;

        var vehicleRot = _transform.GetWorldRotation(vehicle);

        var aimChecked = false;
        if (args.ToCoordinates != null &&
            TryGetTurretOrigin(targetUid, out var originCoords))
        {
            var originMap = _transform.ToMapCoordinates(originCoords);
            var targetMap = _transform.ToMapCoordinates(args.ToCoordinates.Value);
            var direction = targetMap.Position - originMap.Position;
            if (direction.LengthSquared() > 0.0001f)
            {
                aimChecked = true;
                var desiredWorldRotation = direction.ToWorldAngle();
                var currentWorldRotation = (targetTurret.WorldRotation + vehicleRot).Reduced();
                var desiredDelta = Angle.ShortestDistance(currentWorldRotation, desiredWorldRotation);
                if (Math.Abs(desiredDelta.Theta) > alignmentTolerance)
                {
                    args.Cancelled = true;
                    args.ResetCooldown = true;
                    return;
                }
            }
        }

        if (!aimChecked)
        {
            var worldRotation = (targetTurret.WorldRotation + vehicleRot).Reduced();
            var targetWorldRotation = targetTurret.StabilizedRotation
                ? targetTurret.TargetRotation
                : (targetTurret.TargetRotation + vehicleRot).Reduced();

            var delta = Angle.ShortestDistance(worldRotation, targetWorldRotation);
            if (Math.Abs(delta.Theta) > alignmentTolerance)
            {
                args.Cancelled = true;
                args.ResetCooldown = true;
                return;
            }
        }

        ApplyShotDirectionConstraint(ent.Comp, targetUid, targetTurret, vehicle, ref args);

        if (args.ToCoordinates is { } finalTarget && TryComp(ent.Owner, out GunComponent? gun))
        {
#pragma warning disable RA0002
            gun.ShootCoordinates = finalTarget;
#pragma warning restore RA0002
        }
    }

    private bool IsTurretFunctional(EntityUid turretUid)
    {
        if (TryComp(turretUid, out HardpointIntegrityComponent? integrity) &&
            integrity.Integrity <= 0f)
        {
            return false;
        }

        if (!HasComp<VehicleTurretAttachmentComponent>(turretUid))
            return true;

        if (!TryGetParentTurret(turretUid, out var parentUid, out _))
            return true;

        return !TryComp(parentUid, out HardpointIntegrityComponent? parentIntegrity) || parentIntegrity.Integrity > 0f;
    }

    private void ApplyShotDirectionConstraint(
        VehicleTurretComponent sourceTurret,
        EntityUid rotationTurretUid,
        VehicleTurretComponent rotationTurret,
        EntityUid vehicle,
        ref AttemptShootEvent args)
    {
        var muzzleMap = _transform.ToMapCoordinates(args.FromCoordinates);
        if (args.ToCoordinates is not { } currentTarget)
            return;

        var targetMap = _transform.ToMapCoordinates(currentTarget);
        if (targetMap.MapId != muzzleMap.MapId)
            return;

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var barrelWorldRotation = (rotationTurret.WorldRotation + vehicleRot).Reduced();

        const float MinBarrelForwardDist = 0.5f;
        var barrelDir = barrelWorldRotation.ToWorldVec();
        var forwardDist = Vector2.Dot(targetMap.Position - muzzleMap.Position, barrelDir);
        if (forwardDist < MinBarrelForwardDist)
        {
            targetMap = new MapCoordinates(muzzleMap.Position + barrelDir * MinBarrelForwardDist, targetMap.MapId);
            args = args with { ToCoordinates = _transform.ToCoordinates(targetMap) };
        }

        var maxCurveDegrees = MathF.Max(0f, sourceTurret.MaxShotCurvatureDegrees);
        if (!sourceTurret.UseBarrelDirectionForShots && maxCurveDegrees <= 0f)
            return;

        var shotWorldRotation = barrelWorldRotation;

        if (!sourceTurret.UseBarrelDirectionForShots && maxCurveDegrees > 0f)
        {
            var desiredWorldRotation = (targetMap.Position - muzzleMap.Position).ToWorldAngle();
            var maxCurveRadians = MathHelper.DegreesToRadians(maxCurveDegrees);
            var delta = Angle.ShortestDistance(barrelWorldRotation, desiredWorldRotation);
            var clamped = MathHelper.Clamp((float) delta.Theta, -maxCurveRadians, maxCurveRadians);
            shotWorldRotation = (barrelWorldRotation + clamped).Reduced();
        }

        var shotOriginMap = muzzleMap;

        var distance = (targetMap.Position - shotOriginMap.Position).Length();
        if (distance <= 0.0001f)
            return;

        var adjustedMap = new MapCoordinates(
            shotOriginMap.Position + shotWorldRotation.ToWorldVec() * distance,
            muzzleMap.MapId);
        var adjustedTarget = _transform.ToCoordinates(adjustedMap);
        args = args with { ToCoordinates = adjustedTarget };
    }

    private void ApplyPendingTargetRotation(EntityUid turretUid, VehicleTurretComponent turret, EntityUid vehicle)
    {
        if (turret.PendingTargetRotation is not { } pending)
            return;

        if (_timing.CurTime < turret.PendingTargetApplyAt)
            return;

        turret.PendingTargetRotation = null;
        turret.PendingTargetApplyAt = TimeSpan.Zero;

        var sign = turret.PendingDirectionSign;
        turret.PendingDirectionSign = 0;

        ApplyTargetRotation(turretUid, turret, vehicle, pending, sign);
    }

    private void SetTargetRotation(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        EntityUid vehicle,
        Angle desiredRotation,
        bool allowReverseDelay)
    {
        var delta = Angle.ShortestDistance(turret.TargetRotation, desiredRotation);
        var deadzone = MathHelper.DegreesToRadians(MathF.Max(0f, turret.RotationInputDeadzoneDegrees));

        if (Math.Abs(delta.Theta) <= deadzone)
            return;

        var directionSign = Math.Sign(delta.Theta);

        if (allowReverseDelay &&
            turret.ReverseDirectionDelay > 0f &&
            directionSign != 0 &&
            turret.LastAppliedDirectionSign != 0 &&
            directionSign != turret.LastAppliedDirectionSign)
        {
            if (turret.PendingTargetRotation == null || turret.PendingDirectionSign != directionSign)
                turret.PendingTargetApplyAt = _timing.CurTime + TimeSpan.FromSeconds(turret.ReverseDirectionDelay);

            turret.PendingTargetRotation = desiredRotation;
            turret.PendingDirectionSign = directionSign;
            return;
        }

        turret.PendingTargetRotation = null;
        turret.PendingTargetApplyAt = TimeSpan.Zero;
        turret.PendingDirectionSign = 0;
        ApplyTargetRotation(turretUid, turret, vehicle, desiredRotation, directionSign);
    }

    private void ApplyTargetRotation(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        EntityUid vehicle,
        Angle desiredRotation,
        int directionSign)
    {
        var changed = false;

        if (turret.TargetRotation != desiredRotation)
        {
            turret.TargetRotation = desiredRotation;
            changed = true;
        }

        if (directionSign != 0)
            turret.LastAppliedDirectionSign = directionSign;

        if (turret.RotationSpeed <= 0f)
        {
            var vehicleRot = _transform.GetWorldRotation(vehicle);
            var desiredLocal = turret.StabilizedRotation
                ? (desiredRotation - vehicleRot).Reduced()
                : desiredRotation;

            if (turret.WorldRotation != desiredLocal)
            {
                turret.WorldRotation = desiredLocal;
                changed = true;
            }
        }

        if (changed)
            Dirty(turretUid, turret);
    }

    private bool IsLocallyOperatedTurret(EntityUid turretUid)
    {
        if (_player.LocalEntity is not { } local)
            return false;

        if (!TryComp(local, out VehicleWeaponsOperatorComponent? operatorComp) ||
            operatorComp.SelectedWeapon is not { } selectedWeapon)
        {
            return false;
        }

        return TryResolveRotationTarget(selectedWeapon, out var targetUid, out _) &&
               targetUid == turretUid;
    }

    private bool CanOperatorUseTurret(EntityUid turretUid, EntityUid user)
    {
        if (!TryGetVehicle(turretUid, out var vehicle))
            return false;

        if (!TryComp(user, out VehicleWeaponsOperatorComponent? operatorComp) ||
            operatorComp.Vehicle != vehicle)
            return true;

        if (operatorComp.SelectedWeapon != turretUid)
            return false;

        if (TryComp(user, out VehicleViewToggleComponent? viewToggle) && !viewToggle.IsOutside)
            return false;

        return true;
    }

    private static bool ShouldUpdateTransforms(VehicleTurretComponent turret)
    {
        if (turret.RotateToCursor || turret.ShowOverlay || turret.UseDirectionalOffsets)
            return true;

        return turret.PixelOffset != Vector2.Zero ||
               turret.PixelOffsetNorth != Vector2.Zero ||
               turret.PixelOffsetEast != Vector2.Zero ||
               turret.PixelOffsetSouth != Vector2.Zero ||
               turret.PixelOffsetWest != Vector2.Zero;
    }
}

[Serializable, NetSerializable]
public sealed class VehicleTurretRotateEvent : EntityEventArgs
{
    public NetEntity Turret;
    public NetCoordinates Coordinates;
}
