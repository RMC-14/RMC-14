using System;
using System.Numerics;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleTurretSystem : EntitySystem
{
    private const float PixelsPerMeter = 32f;
    private const float FireAlignmentToleranceDegrees = 2f;

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(GridVehicleMoverSystem));
        SubscribeLocalEvent<VehicleTurretComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<VehicleTurretComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<VehicleTurretComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VehicleTurretComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeNetworkEvent<VehicleTurretRotateEvent>(OnRotateEvent);
    }

    public override void Update(float frameTime)
    {
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

        var turretUid = GetEntity(args.Turret);
        if (!TryComp(turretUid, out VehicleTurretComponent? turret))
            return;

        if (!TryGetVehicle(turretUid, out var vehicle))
            return;

        if (!_net.IsClient)
        {
            if (session.SenderSession.AttachedEntity is not { } user)
                return;

            if (TryComp(user, out RMCVehicleViewToggleComponent? viewToggle) && !viewToggle.IsOutside)
                return;

            if (!TryComp(vehicle, out RMCVehicleWeaponsComponent? weapons) ||
                weapons.Operator != user ||
                weapons.SelectedWeapon != turretUid)
            {
                return;
            }
        }

        if (!TryResolveRotationTarget(turretUid, turret, out var targetUid, out var targetTurret))
            return;

        if (!targetTurret.RotateToCursor)
            return;

        if (!TryGetTurretOrigin(targetUid, targetTurret, out var originCoords))
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
    }

    private void EnsureVisual(EntityUid turretUid, VehicleTurretComponent turret, EntityUid vehicle)
    {
        if (_net.IsClient || !turret.ShowOverlay)
            return;

        if (turret.VisualEntity is { } existing && Exists(existing))
            return;

        var visual = Spawn("RMCVehicleTurretVisual", Transform(vehicle).Coordinates);
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
                    var snappedAngle = GetDirectionalAngle(dir);
                    relativeAnchorOffset = (-snappedAngle).RotateVec(worldOffset);
                    turretLocalOffset = (localRot - snappedAngle).RotateVec(worldOffset);
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

    private void TryGetAnchorTurret(
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

    public bool TryGetTurretOrigin(EntityUid turretUid, VehicleTurretComponent turret, out EntityCoordinates origin)
    {
        origin = default;

        if (HasComp<VehicleTurretAttachmentComponent>(turretUid) &&
            TryGetParentTurret(turretUid, out var parentUid, out var parentTurret))
        {
            turretUid = parentUid;
            turret = parentTurret;
        }

        if (!TryGetVehicle(turretUid, out var vehicle))
            return false;

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var baseFacingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
        var facingAngle = GetOffsetFacing(turret, turret, vehicleRot, baseFacingAngle);
        var offset = GetPixelOffset(turret, facingAngle) / PixelsPerMeter;
        var baseCoords = _transform.GetMoverCoordinates(vehicle);
        origin = baseCoords.Offset(offset);
        return true;
    }

    private Vector2 GetPixelOffset(VehicleTurretComponent turret, Angle facing)
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
        return facing.GetCardinalDir();
    }

    private static Direction GetDirectionalDir(float normalized)
    {
        return new Angle(normalized).GetCardinalDir();
    }

    private static Angle GetDirectionalAngle(Direction dir)
    {
        return dir.ToAngle();
    }

    private Angle GetVehicleFacingAngle(EntityUid vehicle, Angle vehicleRot)
    {
        if (TryComp(vehicle, out GridVehicleMoverComponent? mover) && mover.CurrentDirection != Vector2i.Zero)
            return new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

        return vehicleRot;
    }

    private Angle GetOffsetFacing(
        VehicleTurretComponent turret,
        VehicleTurretComponent anchorTurret,
        Angle vehicleRot,
        Angle baseFacingAngle)
    {
        if (!turret.OffsetRotatesWithTurret)
            return baseFacingAngle;

        return (vehicleRot + anchorTurret.WorldRotation).Reduced();
    }

    private bool TryGetVehicle(EntityUid turretUid, out EntityUid vehicle)
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

    private bool TryGetParentTurret(
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

        if (!TryGetTurretOrigin(targetUid, targetTurret, out var originCoords))
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
        Dirty(turretUid, turret);
    }

    private void UpdateTurretRotation(EntityUid turretUid, VehicleTurretComponent turret, EntityUid vehicle, float frameTime)
    {
        if (!turret.RotateToCursor)
            return;

        var vehicleRot = _transform.GetWorldRotation(vehicle);
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

        if (args.ToCoordinates != null &&
            TryGetTurretOrigin(targetUid, targetTurret, out var originCoords))
        {
            var originMap = _transform.ToMapCoordinates(originCoords);
            var targetMap = _transform.ToMapCoordinates(args.ToCoordinates.Value);
            var direction = targetMap.Position - originMap.Position;
            if (direction.LengthSquared() > 0.0001f)
            {
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

        var worldRotation = (targetTurret.WorldRotation + vehicleRot).Reduced();
        var targetWorldRotation = targetTurret.StabilizedRotation
            ? targetTurret.TargetRotation
            : (targetTurret.TargetRotation + vehicleRot).Reduced();

        var delta = Angle.ShortestDistance(worldRotation, targetWorldRotation);
        if (Math.Abs(delta.Theta) <= alignmentTolerance)
            return;

        args.Cancelled = true;
        args.ResetCooldown = true;
    }

    private bool CanOperatorUseTurret(EntityUid turretUid, EntityUid user)
    {
        if (!TryGetVehicle(turretUid, out var vehicle))
            return true;

        if (!TryComp(vehicle, out RMCVehicleWeaponsComponent? weapons) || weapons.Operator != user)
            return true;

        if (TryComp(user, out RMCVehicleViewToggleComponent? viewToggle) && !viewToggle.IsOutside)
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
