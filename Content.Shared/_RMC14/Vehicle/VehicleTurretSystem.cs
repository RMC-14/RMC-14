using System;
using System.Numerics;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleTurretSystem : EntitySystem
{
    private const float PixelsPerMeter = 32f;

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleTurretComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<VehicleTurretComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<VehicleTurretComponent, ComponentShutdown>(OnShutdown);
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
        if (_net.IsClient)
            return;

        if (session.SenderSession.AttachedEntity is not { } user)
            return;

        var turretUid = GetEntity(args.Turret);
        if (!TryComp(turretUid, out VehicleTurretComponent? turret))
            return;

        if (!TryGetVehicle(turretUid, out var vehicle))
            return;

        if (!TryComp(vehicle, out RMCVehicleWeaponsComponent? weapons) ||
            weapons.Operator != user ||
            weapons.SelectedWeapon != turretUid)
        {
            return;
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

        targetTurret.WorldRotation = direction.ToWorldAngle();
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
        var direction = GetVehicleDirection(vehicle);
        var vehicleRot = _transform.GetWorldRotation(vehicle);
        var anchorLocalOffset = (-vehicleRot).RotateVec(GetPixelOffset(anchorTurret, direction) / PixelsPerMeter);
        var localRot = anchorTurret.RotateToCursor ? anchorTurret.WorldRotation - vehicleRot : Angle.Zero;

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
            var turretLocalOffset = (-vehicleRot).RotateVec(GetPixelOffset(turret, direction) / PixelsPerMeter);
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

        var direction = GetVehicleDirection(vehicle);
        var offset = GetPixelOffset(turret, direction) / PixelsPerMeter;
        var baseCoords = _transform.GetMoverCoordinates(vehicle);
        origin = baseCoords.Offset(offset);
        return true;
    }

    private Vector2 GetPixelOffset(VehicleTurretComponent turret, Direction direction)
    {
        if (!turret.UseDirectionalOffsets)
            return turret.PixelOffset;

        var directional = direction switch
        {
            Direction.North => turret.PixelOffsetNorth,
            Direction.East => turret.PixelOffsetEast,
            Direction.South => turret.PixelOffsetSouth,
            Direction.West => turret.PixelOffsetWest,
            _ => Vector2.Zero
        };

        return turret.PixelOffset + directional;
    }

    private Direction GetVehicleDirection(EntityUid vehicle)
    {
        if (TryComp(vehicle, out GridVehicleMoverComponent? mover) && mover.CurrentDirection != Vector2i.Zero)
            return mover.CurrentDirection.AsDirection();

        return _transform.GetWorldRotation(vehicle).GetCardinalDir();
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
            return;

        turret.WorldRotation = _transform.GetWorldRotation(vehicle);
        Dirty(turretUid, turret);
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
