using System;
using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Vehicle;

public sealed class VehicleTurretVisualSystem : EntitySystem
{
    private const float PixelsPerMeter = 32f;

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(VehicleTurretSystem));
        SubscribeLocalEvent<VehicleTurretVisualComponent, ComponentInit>(OnVisualInit);
        SubscribeLocalEvent<VehicleTurretVisualComponent, AfterAutoHandleStateEvent>(OnVisualState);
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<VehicleTurretVisualComponent>();
        while (query.MoveNext(out var uid, out var visual))
        {
            if (!TryGetEntity(visual.Turret, out var turretUid))
                continue;

            if (!TryComp(turretUid, out VehicleTurretComponent? turret))
                continue;

            if (!TryComputeRenderedTransform((EntityUid) turretUid,
                    turret,
                    out _,
                    out _,
                    out var localOffset,
                    out var localRotation))
            {
                continue;
            }

            var visualXform = Transform(uid);
            visualXform.ActivelyLerping = false;
            _transform.SetLocalRotationNoLerp(uid, localRotation, visualXform);
            _transform.SetLocalPositionNoLerp(uid, localOffset, visualXform);
        }
    }

    public bool TryGetRenderedPose(EntityUid turretUid, out EntityCoordinates origin, out Angle worldRotation)
    {
        origin = default;
        worldRotation = Angle.Zero;

        if (!TryComp(turretUid, out VehicleTurretComponent? turret))
            return false;

        if (!TryComputeRenderedTransform(turretUid,
                turret,
                out var vehicle,
                out var vehicleRotation,
                out var localOffset,
                out var localRotation))
        {
            return false;
        }

        origin = _transform.GetMoverCoordinates(new EntityCoordinates(vehicle, localOffset));
        worldRotation = (vehicleRotation + localRotation).Reduced();
        return true;
    }

    private void OnVisualInit(Entity<VehicleTurretVisualComponent> ent, ref ComponentInit args)
    {
        UpdateVisual(ent);
    }

    private void OnVisualState(Entity<VehicleTurretVisualComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisual(ent);
    }

    private void UpdateVisual(Entity<VehicleTurretVisualComponent> ent)
    {
        if (!TryComp(ent.Owner, out SpriteComponent? sprite))
            return;

        if (!TryGetEntity(ent.Comp.Turret, out var turretUid))
            return;

        if (TryComp(turretUid, out VehicleTurretComponent? turret) &&
            !string.IsNullOrWhiteSpace(turret.OverlayState))
        {
            SetOverlayDepth((EntityUid)turretUid, sprite);
            var overlayState = turret.OverlayState;
            if (!string.IsNullOrWhiteSpace(turret.OverlayRsi))
                sprite.LayerSetState(0, overlayState, turret.OverlayRsi);
            else
                sprite.LayerSetState(0, overlayState);

            sprite.LayerSetVisible(0, true);
            return;
        }

        if (!TryComp(turretUid, out SpriteComponent? turretSprite))
            return;

        if (turretSprite.BaseRSI == null || !turretSprite.AllLayers.Any())
            return;

        SetOverlayDepth((EntityUid)turretUid, sprite);
        var state = turretSprite.LayerGetState(0).ToString();
        sprite.LayerSetRSI(0, turretSprite.BaseRSI);
        sprite.LayerSetState(0, state);
        sprite.LayerSetVisible(0, true);
    }

    private void SetOverlayDepth(EntityUid turretUid, SpriteComponent sprite)
    {
        var depth = (int) DrawDepth.OverMobs;
        if (HasComp<VehicleTurretAttachmentComponent>(turretUid))
            depth += 1;

        if (sprite.DrawDepth != depth)
            sprite.DrawDepth = depth;
    }

    private bool TryComputeRenderedTransform(
        EntityUid turretUid,
        VehicleTurretComponent turret,
        out EntityUid vehicle,
        out Angle vehicleRot,
        out Vector2 localOffset,
        out Angle localRotation)
    {
        vehicle = default;
        vehicleRot = Angle.Zero;
        localOffset = Vector2.Zero;
        localRotation = Angle.Zero;

        if (!TryGetVehicle(turretUid, out vehicle))
            return false;

        TryGetAnchorTurret(turretUid, turret, out var anchorUid, out var anchorTurret);

        vehicleRot = _transform.GetWorldRotation(vehicle);
        var eyeRot = _eye.CurrentEye.Rotation;
        var baseFacingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
        var anchorFacingAngle = GetRenderFacing(anchorTurret, anchorTurret, vehicleRot, baseFacingAngle, eyeRot);
        var anchorPixelOffset = GetPixelOffset(anchorTurret, anchorFacingAngle) / PixelsPerMeter;
        var anchorLocalOffset = GetVehicleLocalOffset(anchorTurret, anchorPixelOffset, vehicleRot, eyeRot);

        var targetLocalRotation = anchorTurret.RotateToCursor ? anchorTurret.WorldRotation : Angle.Zero;
        localOffset = anchorLocalOffset;
        localRotation = targetLocalRotation;

        if (anchorUid == turretUid)
            return true;

        var turretFacingAngle = GetRenderFacing(turret, anchorTurret, vehicleRot, baseFacingAngle, eyeRot);
        var worldOffset = GetPixelOffset(turret, turretFacingAngle) / PixelsPerMeter;
        Vector2 turretLocalOffset;

        if (turret.OffsetRotatesWithTurret)
        {
            if (turret.UseDirectionalOffsets)
            {
                var dir = GetDirectionalDir(turretFacingAngle);
                var snappedAngle = GetDirectionalAngle(dir);
                turretLocalOffset = (targetLocalRotation - snappedAngle).RotateVec(worldOffset);
            }
            else
            {
                turretLocalOffset = targetLocalRotation.RotateVec(worldOffset);
            }
        }
        else
        {
            turretLocalOffset = GetVehicleLocalOffset(turret, worldOffset, vehicleRot, eyeRot);
        }

        localOffset += turretLocalOffset;
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

    private Angle GetRenderFacing(
        VehicleTurretComponent turret,
        VehicleTurretComponent anchorTurret,
        Angle vehicleRot,
        Angle baseFacingAngle,
        Angle eyeRot)
    {
        return (GetOffsetFacing(turret, anchorTurret, vehicleRot, baseFacingAngle) + eyeRot).Reduced();
    }

    private static Vector2 GetVehicleLocalOffset(
        VehicleTurretComponent turret,
        Vector2 offset,
        Angle vehicleRot,
        Angle eyeRot)
    {
        if (turret.UseDirectionalOffsets)
            offset = (-eyeRot).RotateVec(offset);

        return (-vehicleRot).RotateVec(offset);
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

    private Angle GetVehicleFacingAngle(EntityUid vehicle, Angle vehicleRot)
    {
        if (TryComp(vehicle, out GridVehicleMoverComponent? mover) && mover.CurrentDirection != Vector2i.Zero)
            return new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

        return vehicleRot;
    }
}
