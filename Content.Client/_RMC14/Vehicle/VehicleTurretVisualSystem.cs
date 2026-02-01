using System;
using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Vehicle.Components;
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
        var eyeRotation = _eye.CurrentEye.Rotation;
        var query = EntityQueryEnumerator<VehicleTurretVisualComponent>();
        while (query.MoveNext(out var uid, out var visual))
        {
            if (!TryGetEntity(visual.Turret, out var turretUid))
                continue;

            if (!TryComp(turretUid, out VehicleTurretComponent? turret))
                continue;

            if (!TryGetVehicle((EntityUid)turretUid, out var vehicle))
                continue;

            TryGetAnchorTurret((EntityUid)turretUid, turret, out var anchorUid, out var anchorTurret);

            var vehicleRot = _transform.GetWorldRotation(vehicle);
            var relativeRotation = (vehicleRot + eyeRotation).Reduced().FlipPositive();
            var offsetPixels = GetPixelOffset(anchorTurret, relativeRotation);
            if (anchorUid != turretUid)
                offsetPixels += GetPixelOffset(turret, relativeRotation);
            var offsetWorld = (-eyeRotation).RotateVec(offsetPixels / PixelsPerMeter);
            var targetLocalOffset = (-vehicleRot).RotateVec(offsetWorld);

            var targetLocalRotation = Angle.Zero;
            if (anchorTurret.RotateToCursor)
                targetLocalRotation = anchorTurret.WorldRotation;

            var localOffset = targetLocalOffset;
            var localRotation = targetLocalRotation;

            var visualXform = Transform(uid);
            _transform.SetLocalRotationNoLerp(uid, localRotation, visualXform);
            _transform.SetLocalPositionNoLerp(uid, localOffset, visualXform);
        }
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

        var state = turretSprite.LayerGetState(0).ToString();
        sprite.LayerSetRSI(0, turretSprite.BaseRSI);
        sprite.LayerSetState(0, state);
        sprite.LayerSetVisible(0, true);
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
}
