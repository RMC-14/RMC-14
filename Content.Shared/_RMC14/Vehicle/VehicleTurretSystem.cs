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

        if (Exists(visual))
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
        var facingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
        var anchorLocalOffset = (-vehicleRot).RotateVec(GetPixelOffset(anchorTurret, facingAngle) / PixelsPerMeter);
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
            var turretLocalOffset = (-vehicleRot).RotateVec(GetPixelOffset(turret, facingAngle) / PixelsPerMeter);
            var relativeAnchorOffset = (-localRot).RotateVec(turretLocalOffset);
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
        var facingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
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

        var segment = MathHelper.PiOver2;
        var index = (int) Math.Floor(normalized / segment) & 3;
        var t = (float) ((normalized - index * segment) / segment);

        var current = baseOffset + GetDirectionalOffset(turret, index);
        var next = baseOffset + GetDirectionalOffset(turret, (index + 1) & 3);
        return Vector2.Lerp(current, next, t);
    }

    private static Vector2 GetDirectionalOffset(VehicleTurretComponent turret, int index)
    {
        return index switch
        {
            0 => turret.PixelOffsetSouth,
            1 => turret.PixelOffsetEast,
            2 => turret.PixelOffsetNorth,
            3 => turret.PixelOffsetWest,
            _ => Vector2.Zero
        };
    }

    private Angle GetVehicleFacingAngle(EntityUid vehicle, Angle vehicleRot)
    {
        if (TryComp(vehicle, out GridVehicleMoverComponent? mover) && mover.CurrentDirection != Vector2i.Zero)
            return new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

        return vehicleRot;
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

        if (!TryResolveRotationTarget(ent.Owner, ent.Comp, out var targetUid, out var targetTurret))
            return;

        if (!targetTurret.RotateToCursor)
            return;

        if (!TryGetVehicle(targetUid, out var vehicle))
            return;

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var worldRotation = (targetTurret.WorldRotation + vehicleRot).Reduced();
        var targetWorldRotation = targetTurret.StabilizedRotation
            ? targetTurret.TargetRotation
            : (targetTurret.TargetRotation + vehicleRot).Reduced();

        var delta = Angle.ShortestDistance(worldRotation, targetWorldRotation);
        if (Math.Abs(delta.Theta) <= MathHelper.DegreesToRadians(FireAlignmentToleranceDegrees))
            return;

        args.Cancelled = true;
        args.ResetCooldown = true;
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
