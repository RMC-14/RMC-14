using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client._RMC14.Vehicle;
using Content.Client.Resources;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Vehicle
{
    public sealed class VehicleHardpointDebugOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV | OverlaySpace.ScreenSpace;

        private const float PixelsPerMeter = 32f;
        private static readonly Vector2 GunLabelWorldOffset = new(0.18f, -0.18f);
        private static readonly Vector2 TurretLabelWorldOffset = new(-0.55f, -0.18f);
        private const float LabelLineHeight = 13f;
        private const float LabelPadding = 4f;
        private const float LabelCharWidth = 7f;

        public bool Enabled { get; set; }

        private readonly IEntityManager _ents;
        private readonly IEyeManager _eye;
        private readonly SharedTransformSystem _transform;
        private readonly SharedContainerSystem _container;
        private readonly VehicleTurretMuzzleOffsetSystem _vehicleTurretMuzzle;
        private readonly VehicleTurretVisualSystem _vehicleTurretVisual;
        private readonly Font _font;
        private readonly EntityQuery<GunComponent> _gunQ;
        private readonly EntityQuery<GunFireArcComponent> _fireArcQ;
        private readonly EntityQuery<GridVehicleMoverComponent> _moverQ;
        private readonly EntityQuery<GunMuzzleOffsetComponent> _muzzleQ;
        private readonly EntityQuery<VehicleTurretComponent> _turretQ;
        private readonly EntityQuery<VehicleTurretMuzzleComponent> _turretMuzzleQ;
        private readonly EntityQuery<VehiclePortGunComponent> _portGunQ;
        private readonly List<DebugLabel> _labels = new();

        public VehicleHardpointDebugOverlay(IEntityManager ents)
        {
            _ents = ents;
            _eye = IoCManager.Resolve<IEyeManager>();
            var cache = IoCManager.Resolve<IResourceCache>();
            _transform = ents.System<SharedTransformSystem>();
            _container = ents.System<SharedContainerSystem>();
            _vehicleTurretMuzzle = ents.System<VehicleTurretMuzzleOffsetSystem>();
            _vehicleTurretVisual = ents.System<VehicleTurretVisualSystem>();
            _font = cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
            _gunQ = ents.GetEntityQuery<GunComponent>();
            _fireArcQ = ents.GetEntityQuery<GunFireArcComponent>();
            _moverQ = ents.GetEntityQuery<GridVehicleMoverComponent>();
            _muzzleQ = ents.GetEntityQuery<GunMuzzleOffsetComponent>();
            _turretQ = ents.GetEntityQuery<VehicleTurretComponent>();
            _turretMuzzleQ = ents.GetEntityQuery<VehicleTurretMuzzleComponent>();
            _portGunQ = ents.GetEntityQuery<VehiclePortGunComponent>();
        }

        private readonly record struct DebugLine(string Text, Color Color);
        private readonly record struct DebugLabel(Vector2 WorldPosition, List<DebugLine> Lines);

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (!Enabled)
                return;

            if (args.Space == OverlaySpace.ScreenSpace)
            {
                DrawLabels(args);
                return;
            }

            _labels.Clear();
            var handle = args.WorldHandle;
            var query = _ents.EntityQueryEnumerator<GunMuzzleOffsetComponent>();

            while (query.MoveNext(out var uid, out var muzzle))
            {
                if (!_turretQ.HasComp(uid) && !_portGunQ.HasComp(uid))
                    continue;

                if (!TryGetMuzzlePositions(uid, muzzle, args.MapId, out var origin, out var muzzlePos))
                    continue;

                handle.DrawLine(origin, muzzlePos, new Color(0.95f, 0.95f, 0.95f, 0.7f));
                handle.DrawCircle(origin, 0.07f, new Color(0.2f, 0.9f, 1f, 0.9f));
                handle.DrawCircle(muzzlePos, 0.1f, new Color(1f, 0.75f, 0.2f, 0.95f));

                DrawShootArc(uid, origin, args.MapId, handle);
                DrawShootTarget(uid, origin, args.MapId, handle);
                AddGunDebugLabel(uid, muzzle, origin, muzzlePos);
            }

            var turretMuzzleQuery = _ents.EntityQueryEnumerator<VehicleTurretMuzzleComponent>();
            while (turretMuzzleQuery.MoveNext(out var uid, out var turretMuzzle))
            {
                if (!_turretQ.HasComp(uid))
                    continue;

                if (!TryGetTurretMuzzlePositions(uid, turretMuzzle, args.MapId, out var turretBasePos, out var basePos, out var leftPos, out var rightPos, out var leftRadius, out var rightRadius, out var useRightNext))
                    continue;

                if (leftRadius > 0f)
                    handle.DrawCircle(turretBasePos, leftRadius, new Color(0.25f, 0.85f, 1f, 0.5f));
                if (rightRadius > 0f && MathF.Abs(rightRadius - leftRadius) > 0.01f)
                    handle.DrawCircle(turretBasePos, rightRadius, new Color(1f, 0.6f, 0.2f, 0.5f));

                var leftColor = useRightNext ? new Color(0.4f, 0.4f, 0.4f, 0.7f) : new Color(0.2f, 0.95f, 0.4f, 0.95f);
                var rightColor = useRightNext ? new Color(0.2f, 0.95f, 0.4f, 0.95f) : new Color(0.4f, 0.4f, 0.4f, 0.7f);
                if (leftRadius > 0f)
                    handle.DrawCircle(leftPos, 0.08f, leftColor);
                if (rightRadius > 0f)
                    handle.DrawCircle(rightPos, 0.08f, rightColor);
                handle.DrawLine(turretBasePos, basePos, new Color(0.4f, 0.9f, 1f, 0.5f));
            }

            var turretQuery = _ents.EntityQueryEnumerator<VehicleTurretComponent>();
            while (turretQuery.MoveNext(out var uid, out var turret))
            {
                if (!TryGetTurretOverlayPositions(uid, turret, args.MapId, out var basePos, out var anchorPos, out var turretPos))
                    continue;

                handle.DrawCircle(basePos, 0.06f, new Color(0.7f, 0.7f, 0.7f, 0.8f));
                handle.DrawLine(basePos, anchorPos, new Color(0.2f, 0.8f, 0.95f, 0.7f));
                handle.DrawCircle(anchorPos, 0.07f, new Color(0.2f, 0.8f, 0.95f, 0.9f));

                if (anchorPos != turretPos)
                {
                    handle.DrawLine(anchorPos, turretPos, new Color(1f, 0.7f, 0.2f, 0.8f));
                    handle.DrawCircle(turretPos, 0.08f, new Color(1f, 0.7f, 0.2f, 0.95f));
                }

                if (!_muzzleQ.HasComp(uid))
                    AddTurretDebugLabel(uid, turret, basePos, anchorPos, turretPos);
            }
        }

        private void DrawLabels(in OverlayDrawArgs args)
        {
            if (args.ViewportControl == null || _labels.Count == 0)
                return;

            var ordered = new List<(DebugLabel Label, Vector2 Position, float Width, float Height)>(_labels.Count);
            foreach (var label in _labels)
            {
                var screenPos = args.ViewportControl.WorldToScreen(label.WorldPosition);
                var width = GetApproxLabelWidth(label.Lines);
                var height = label.Lines.Count * LabelLineHeight;
                ordered.Add((label, screenPos, width, height));
            }

            ordered.Sort((a, b) =>
            {
                var y = a.Position.Y.CompareTo(b.Position.Y);
                return y != 0 ? y : a.Position.X.CompareTo(b.Position.X);
            });

            var occupied = new List<Box2>(ordered.Count);
            foreach (var label in ordered)
            {
                var position = label.Position;
                var rect = GetLabelRect(position, label.Width, label.Height);

                var iterations = 0;
                while (iterations++ < 16)
                {
                    Box2? collision = null;
                    foreach (var other in occupied)
                    {
                        if (rect.Intersects(other))
                        {
                            collision = other;
                            break;
                        }
                    }

                    if (collision == null)
                        break;

                    position.Y = collision.Value.Bottom + LabelPadding;
                    rect = GetLabelRect(position, label.Width, label.Height);
                }

                occupied.Add(rect);
                var linePos = position;

                foreach (var line in label.Label.Lines)
                {
                    args.ScreenHandle.DrawString(_font, linePos + Vector2.One, line.Text, Color.Black);
                    args.ScreenHandle.DrawString(_font, linePos, line.Text, line.Color);
                    linePos.Y += LabelLineHeight;
                }
            }
        }

        private static float GetApproxLabelWidth(List<DebugLine> lines)
        {
            var longest = 0;
            foreach (var line in lines)
            {
                if (line.Text.Length > longest)
                    longest = line.Text.Length;
            }

            return longest * LabelCharWidth;
        }

        private static Box2 GetLabelRect(Vector2 position, float width, float height)
        {
            return Box2.FromDimensions(position - new Vector2(LabelPadding, LabelPadding),
                new Vector2(width + LabelPadding * 2f, height + LabelPadding * 2f));
        }

        private void AddGunDebugLabel(EntityUid uid, GunMuzzleOffsetComponent muzzle, Vector2 origin, Vector2 muzzlePos)
        {
            var lines = new List<DebugLine>();
            var eyeRot = _eye.CurrentEye.Rotation;
            var isTurret = _turretQ.TryComp(uid, out var turret);
            var isPortGun = _portGunQ.HasComp(uid);
            var role = isTurret ? "turret-gun" : isPortGun ? "port-gun" : "gun";
            lines.Add(new DebugLine($"{role} {uid.Id}", Color.White));

            var baseUid = uid;
            var physicalRotation = _transform.GetWorldRotation(uid);
            var visualRotation = physicalRotation;
            EntityUid? vehicleUid = null;
            if (isTurret && turret != null)
            {
                if (_vehicleTurretVisual.TryGetRenderedPose(uid, out _, out var renderedRotation))
                    visualRotation = renderedRotation;

                if (TryGetVehicle(uid, out var vehicle))
                {
                    vehicleUid = vehicle;
                    TryGetAnchorTurret(uid, turret, out var anchorUid, out var anchorTurret);
                    var vehicleRot = _transform.GetWorldRotation(vehicle);
                    var vehicleFacing = GetVehicleFacingAngle(vehicle, vehicleRot);
                    var renderFacing = GetRenderFacing(turret, anchorTurret, vehicleRot, vehicleFacing, eyeRot);
                    var pixelDir = turret.UseDirectionalOffsets ? GetDirectionalDir(renderFacing) : Direction.Invalid;
                    var pixelPreset = turret.UseDirectionalOffsets ? GetDirectionalOffset(turret, pixelDir) : turret.PixelOffset;

                    lines.Add(new DebugLine(
                        $"veh {vehicle.Id} face {vehicleFacing.GetCardinalDir()} eye {FmtDeg(eyeRot)} phys {FmtDeg(physicalRotation)} vis {FmtDeg(visualRotation)}",
                        new Color(0.80f, 0.80f, 0.80f, 1f)));
                    lines.Add(new DebugLine(
                        $"render {(turret.UseDirectionalOffsets ? pixelDir.ToString() : "-")} {FmtVec(pixelPreset)} gun ",
                        new Color(0.55f, 0.90f, 1f, 1f)));
                    lines[^1] = new DebugLine(
                        $"{lines[^1].Text}{(muzzle.UseDirectionalOffsets ? GetBaseDirection(baseUid, physicalRotation).ToString() : "-")} {FmtVec(muzzle.UseDirectionalOffsets ? GetDirectionalGunOffset(muzzle, GetBaseDirection(baseUid, physicalRotation)) : muzzle.Offset)}",
                        new Color(1f, 0.75f, 0.30f, 1f));
                }
            }
            else
            {
                if (muzzle.UseContainerOwner &&
                    _container.TryGetContainingContainer((uid, null), out var container))
                {
                    baseUid = container.Owner;
                }

                physicalRotation = GetBaseRotation(baseUid, muzzle.AngleOffset);
                visualRotation = physicalRotation;
                lines.Add(new DebugLine(
                    $"base {baseUid.Id} rot {FmtDeg(physicalRotation)}",
                    new Color(0.80f, 0.80f, 0.80f, 1f)));
            }

            var selectedOffset = muzzle.Offset;
            var appliedOffset = muzzle.Offset;
            var dirText = "-";
            if (muzzle.UseDirectionalOffsets)
            {
                var dir = GetBaseDirection(baseUid, physicalRotation);
                dirText = dir.ToString();
                selectedOffset = GetDirectionalGunOffset(muzzle, dir);
                appliedOffset = selectedOffset;
            }
            var worldOffset = muzzle.UseDirectionalOffsets && !muzzle.RotateDirectionalOffsets
                ? appliedOffset
                : physicalRotation.RotateVec(appliedOffset);

            var muzzleRotation = physicalRotation;
            Vector2? targetPosition = null;
            if (_gunQ.TryComp(uid, out var gun) &&
                gun.ShootCoordinates is { } targetCoords)
            {
                var targetMap = _transform.ToMapCoordinates(targetCoords);
                targetPosition = targetMap.Position;

                if (muzzle.UseAimDirection)
                {
                    var direction = targetMap.Position - origin;
                    if (direction.LengthSquared() > 0.0001f)
                        muzzleRotation = direction.ToWorldAngle() + muzzle.AngleOffset;
                }
            }

            lines.Add(new DebugLine(
                $"origin {FmtPos(origin)} muzzle {FmtPos(muzzlePos)}",
                new Color(0.30f, 0.90f, 1f, 1f)));
            lines.Add(new DebugLine(
                $"dir {dirText} pick {FmtVec(selectedOffset)} world {FmtVec(worldOffset)}",
                new Color(1f, 0.82f, 0.25f, 1f)));
            if (muzzle.MuzzleOffset != Vector2.Zero || muzzle.UseAimDirection)
            {
                lines.Add(new DebugLine(
                    $"muzzle {FmtVec(muzzle.MuzzleOffset)} rot {FmtDeg(muzzleRotation)} aim={FmtBool(muzzle.UseAimDirection)}",
                    new Color(0.72f, 1f, 0.76f, 1f)));
            }

            if (targetPosition is { } target)
            {
                lines.Add(new DebugLine(
                    $"target {FmtPos(target)} dist {(target - origin).Length():0.00}",
                    new Color(0.95f, 0.40f, 0.95f, 1f)));
            }

            if (_turretMuzzleQ.TryComp(uid, out var turretMuzzle))
            {
                var dir = GetBaseDirection(uid, physicalRotation);
                var left = GetDirectionalTurretOffset(turretMuzzle, dir, false);
                var right = GetDirectionalTurretOffset(turretMuzzle, dir, true);
                lines.Add(new DebugLine(
                    $"alt {(turretMuzzle.UseRightNext ? "R" : "L")} L {FmtVec(left)} R {FmtVec(right)}",
                    new Color(1f, 0.65f, 0.25f, 1f)));
            }

            _labels.Add(new DebugLabel(muzzlePos + GunLabelWorldOffset, lines));
        }

        private void AddTurretDebugLabel(
            EntityUid uid,
            VehicleTurretComponent turret,
            Vector2 basePos,
            Vector2 anchorPos,
            Vector2 turretPos)
        {
            var lines = new List<DebugLine>
            {
                new($"turret {uid.Id}", Color.White),
            };

            if (TryGetVehicle(uid, out var vehicle))
            {
                TryGetAnchorTurret(uid, turret, out var anchorUid, out var anchorTurret);
                var vehicleRot = _transform.GetWorldRotation(vehicle);
                var eyeRot = _eye.CurrentEye.Rotation;
                var vehicleFacing = GetVehicleFacingAngle(vehicle, vehicleRot);
                var renderFacing = GetRenderFacing(turret, anchorTurret, vehicleRot, vehicleFacing, eyeRot);
                var pixelDir = turret.UseDirectionalOffsets ? GetDirectionalDir(renderFacing) : Direction.Invalid;
                var pixelPreset = turret.UseDirectionalOffsets ? GetDirectionalOffset(turret, pixelDir) : turret.PixelOffset;
                var renderedRotation = _transform.GetWorldRotation(uid);

                if (_vehicleTurretVisual.TryGetRenderedPose(uid, out _, out var renderedPoseRotation))
                    renderedRotation = renderedPoseRotation;

                lines.Add(new DebugLine(
                    $"veh {vehicle.Id} face {vehicleFacing.GetCardinalDir()} eye {FmtDeg(eyeRot)} render {FmtDeg(renderedRotation)}",
                    new Color(0.80f, 0.80f, 0.80f, 1f)));
                lines.Add(new DebugLine(
                    $"anchor {anchorUid.Id} world {FmtDeg(turret.WorldRotation)} target {FmtDeg(turret.TargetRotation)}",
                    new Color(0.55f, 0.90f, 1f, 1f)));
                lines.Add(new DebugLine(
                    $"pix {(turret.UseDirectionalOffsets ? pixelDir.ToString() : "-")} {FmtVec(pixelPreset)} base {FmtPos(basePos)} pos {FmtPos(turretPos)}",
                    new Color(0.75f, 0.95f, 0.65f, 1f)));
            }

            _labels.Add(new DebugLabel(turretPos + TurretLabelWorldOffset, lines));
        }

        private static string FmtBool(bool value)
        {
            return value ? "Y" : "N";
        }

        private static string FmtDeg(Angle angle)
        {
            return $"{angle.Degrees:0.0}";
        }

        private static string FmtVec(Vector2 vector)
        {
            return $"<{vector.X:0.00},{vector.Y:0.00}>";
        }

        private static string FmtPos(Vector2 vector)
        {
            return $"{vector.X:0.00},{vector.Y:0.00}";
        }

        private bool TryGetMuzzlePositions(
            EntityUid uid,
            GunMuzzleOffsetComponent muzzle,
            MapId mapId,
            out Vector2 origin,
            out Vector2 muzzlePos)
        {
            origin = default;
            muzzlePos = default;

            if (_turretQ.HasComp(uid) &&
                _vehicleTurretMuzzle.TryGetGunOrigin(uid, null, out var originCoords))
            {
                var originMap = _transform.ToMapCoordinates(originCoords);
                if (originMap.MapId != mapId)
                    return false;

                origin = originMap.Position;
                muzzlePos = origin;
                return true;
            }

            var baseUid = uid;
            if (muzzle.UseContainerOwner &&
                _container.TryGetContainingContainer((uid, null), out var container))
            {
                baseUid = container.Owner;
            }

            if (!_ents.TryGetComponent(baseUid, out TransformComponent? baseXform))
                return false;
            if (baseXform.MapID != mapId)
                return false;

            var baseCoords = _transform.GetMoverCoordinates(baseUid);
            var baseRotation = GetBaseRotation(baseUid, muzzle.AngleOffset);

            var (offset, rotateOffset) = GetOffset(muzzle, baseUid, baseRotation);
            var muzzleOriginCoords = rotateOffset
                ? baseCoords.Offset(baseRotation.RotateVec(offset))
                : baseCoords.Offset(offset);
            origin = _transform.ToMapCoordinates(muzzleOriginCoords).Position;

            var muzzleCoords = muzzleOriginCoords;
            var muzzleRotation = baseRotation;

            if (muzzle.MuzzleOffset != Vector2.Zero)
            {
                if (muzzle.UseAimDirection &&
                    _gunQ.TryComp(uid, out var gun) &&
                    gun.ShootCoordinates is { } shootCoords)
                {
                    var pivotMap = _transform.ToMapCoordinates(muzzleOriginCoords);
                    var targetMap = _transform.ToMapCoordinates(shootCoords);
                    var direction = targetMap.Position - pivotMap.Position;
                    if (direction.LengthSquared() > 0.0001f)
                        muzzleRotation = direction.ToWorldAngle() + muzzle.AngleOffset;
                }

                muzzleCoords = muzzleOriginCoords.Offset(muzzleRotation.RotateVec(muzzle.MuzzleOffset));
            }

            muzzlePos = _transform.ToMapCoordinates(muzzleCoords).Position;
            return true;
        }

        private bool TryGetTurretMuzzlePositions(
            EntityUid uid,
            VehicleTurretMuzzleComponent turretMuzzle,
            MapId mapId,
            out Vector2 turretBasePos,
            out Vector2 basePos,
            out Vector2 leftPos,
            out Vector2 rightPos,
            out float leftRadius,
            out float rightRadius,
            out bool useRightNext)
        {
            turretBasePos = default;
            basePos = default;
            leftPos = default;
            rightPos = default;
            leftRadius = 0f;
            rightRadius = 0f;
            useRightNext = turretMuzzle.UseRightNext;

            if (!_ents.TryGetComponent(uid, out TransformComponent? turretXform))
                return false;
            if (turretXform.MapID != mapId)
                return false;

            var baseRotation = _transform.GetWorldRotation(uid);
            if (_vehicleTurretVisual.TryGetRenderedPose(uid, out var renderedTurretCoords, out var renderedRotation))
            {
                var renderedTurretMap = _transform.ToMapCoordinates(renderedTurretCoords);
                if (renderedTurretMap.MapId != mapId)
                    return false;

                turretBasePos = renderedTurretMap.Position;
                baseRotation = renderedRotation;
            }
            else
            {
                turretBasePos = _transform.ToMapCoordinates(_transform.GetMoverCoordinates(uid)).Position;
            }

            if (!TryGetGunOriginCoordinates(uid, mapId, out var originCoords))
                return false;

            basePos = _transform.ToMapCoordinates(originCoords).Position;
            leftPos = basePos + GetTurretMuzzleWorldOffset(turretMuzzle, baseRotation, useRight: false);
            rightPos = basePos + GetTurretMuzzleWorldOffset(turretMuzzle, baseRotation, useRight: true);

            leftRadius = (leftPos - turretBasePos).Length();
            rightRadius = (rightPos - turretBasePos).Length();

            return true;
        }

        private bool TryGetGunOriginCoordinates(EntityUid uid, MapId mapId, out EntityCoordinates originCoords)
        {
            originCoords = default;

            if (_turretQ.HasComp(uid) &&
                _vehicleTurretMuzzle.TryGetGunOrigin(uid, null, out var gunOrigin))
            {
                var originMap = _transform.ToMapCoordinates(gunOrigin);
                if (originMap.MapId != mapId)
                    return false;

                originCoords = gunOrigin;
                return true;
            }

            var baseUid = uid;
            if (_muzzleQ.TryComp(uid, out var muzzle) &&
                muzzle.UseContainerOwner &&
                _container.TryGetContainingContainer((uid, null), out var container))
            {
                baseUid = container.Owner;
            }

            if (!_ents.TryGetComponent(baseUid, out TransformComponent? baseXform))
                return false;
            if (baseXform.MapID != mapId)
                return false;

            var baseCoords = _transform.GetMoverCoordinates(baseUid);
            if (muzzle == null)
            {
                originCoords = baseCoords;
                return true;
            }

            var baseRotation = GetBaseRotation(baseUid, muzzle.AngleOffset);
            var (offset, rotateOffset) = GetOffset(muzzle, baseUid, baseRotation);
            var fromCoords = rotateOffset
                ? baseCoords.Offset(baseRotation.RotateVec(offset))
                : baseCoords.Offset(offset);

            if (muzzle.MuzzleOffset != Vector2.Zero)
            {
                var muzzleRotation = baseRotation;
                if (muzzle.UseAimDirection &&
                    _gunQ.TryComp(uid, out var gun) &&
                    gun.ShootCoordinates is { } shootCoords)
                {
                    var pivotMap = _transform.ToMapCoordinates(fromCoords);
                    var targetMap = _transform.ToMapCoordinates(shootCoords);
                    var direction = targetMap.Position - pivotMap.Position;
                    if (direction.LengthSquared() > 0.0001f)
                        muzzleRotation = direction.ToWorldAngle() + muzzle.AngleOffset;
                }

                fromCoords = fromCoords.Offset(muzzleRotation.RotateVec(muzzle.MuzzleOffset));
            }

            originCoords = fromCoords;
            return true;
        }

        private bool TryGetTurretOverlayPositions(
            EntityUid turretUid,
            VehicleTurretComponent turret,
            MapId mapId,
            out Vector2 basePos,
            out Vector2 anchorPos,
            out Vector2 turretPos)
        {
            basePos = default;
            anchorPos = default;
            turretPos = default;

            if (!TryGetVehicle(turretUid, out var vehicle))
                return false;

            var baseCoords = _transform.GetMoverCoordinates(vehicle);
            var baseMap = _transform.ToMapCoordinates(baseCoords);
            if (baseMap.MapId != mapId)
                return false;

            TryGetAnchorTurret(turretUid, turret, out var anchorUid, out var anchorTurret);

            var vehicleRot = _transform.GetWorldRotation(vehicle);
            var eyeRot = _eye.CurrentEye.Rotation;
            var baseFacingAngle = GetVehicleFacingAngle(vehicle, vehicleRot);
            var anchorFacingAngle = GetRenderFacing(anchorTurret, anchorTurret, vehicleRot, baseFacingAngle, eyeRot);
            var anchorPixelOffset = GetPixelOffset(anchorTurret, anchorFacingAngle) / PixelsPerMeter;
            var anchorLocalOffset = GetVehicleLocalOffset(anchorTurret, anchorPixelOffset, vehicleRot, eyeRot);
            var anchorCoords = baseCoords.Offset(anchorLocalOffset);

            basePos = baseMap.Position;
            anchorPos = _transform.ToMapCoordinates(anchorCoords).Position;

            if (anchorUid == turretUid)
            {
                turretPos = anchorPos;
                return true;
            }

            var localRot = anchorTurret.RotateToCursor ? anchorTurret.WorldRotation : Angle.Zero;
            var turretFacingAngle = GetRenderFacing(turret, anchorTurret, vehicleRot, baseFacingAngle, eyeRot);
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
            MapCoordinates turretMap;
            if (turret.OffsetRotatesWithTurret)
                turretMap = _transform.ToMapCoordinates(new EntityCoordinates(anchorUid, relativeAnchorOffset));
            else
                turretMap = _transform.ToMapCoordinates(baseCoords.Offset(anchorLocalOffset + relativeAnchorOffset));
            if (turretMap.MapId != mapId)
                return false;

            turretPos = turretMap.Position;
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

            var dir = GetDirectionalDir((float)normalized);
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

        private static Vector2 GetDirectionalGunOffset(GunMuzzleOffsetComponent muzzle, Direction dir)
        {
            return dir switch
            {
                Direction.North => muzzle.OffsetNorth,
                Direction.East => muzzle.OffsetEast,
                Direction.South => muzzle.OffsetSouth,
                Direction.West => muzzle.OffsetWest,
                _ => muzzle.Offset,
            };
        }

        private static Vector2 GetDirectionalTurretOffset(VehicleTurretMuzzleComponent muzzle, Direction dir, bool useRight)
        {
            if (!muzzle.UseDirectionalOffsets)
                return useRight ? muzzle.OffsetRight : muzzle.OffsetLeft;

            return dir switch
            {
                Direction.North => useRight ? muzzle.OffsetRightNorth : muzzle.OffsetLeftNorth,
                Direction.East => useRight ? muzzle.OffsetRightEast : muzzle.OffsetLeftEast,
                Direction.South => useRight ? muzzle.OffsetRightSouth : muzzle.OffsetLeftSouth,
                Direction.West => useRight ? muzzle.OffsetRightWest : muzzle.OffsetLeftWest,
                _ => useRight ? muzzle.OffsetRight : muzzle.OffsetLeft
            };
        }

        private static Vector2 GetTurretMuzzleWorldOffset(
            VehicleTurretMuzzleComponent muzzle,
            Angle baseRotation,
            bool useRight)
        {
            var renderOffset = GetDirectionalTurretOffset(
                muzzle,
                VehicleTurretDirectionHelpers.GetRenderAlignedCardinalDir(baseRotation),
                useRight);
            return baseRotation.RotateVec(renderOffset);
        }

        private void DrawShootArc(EntityUid uid, Vector2 origin, MapId mapId, DrawingHandleWorld handle)
        {
            if (!_fireArcQ.TryComp(uid, out var arc))
                return;

            if (!_container.TryGetContainingContainer((uid, null), out var container))
                return;

            if (!_ents.TryGetComponent(container.Owner, out TransformComponent? baseXform) || baseXform.MapID != mapId)
                return;

            var baseRotation = GetBaseRotation(container.Owner, arc.AngleOffset);
            var halfArc = Angle.FromDegrees(arc.Arc.Degrees / 2f);
            var left = baseRotation + halfArc;
            var right = baseRotation - halfArc;

            const float arcLength = 3.5f;
            handle.DrawLine(origin, origin + baseRotation.ToWorldVec() * arcLength, new Color(0.2f, 0.9f, 0.3f, 0.8f));
            handle.DrawLine(origin, origin + left.ToWorldVec() * arcLength, new Color(0.95f, 0.45f, 0.2f, 0.8f));
            handle.DrawLine(origin, origin + right.ToWorldVec() * arcLength, new Color(0.95f, 0.45f, 0.2f, 0.8f));
        }

        private void DrawShootTarget(EntityUid uid, Vector2 origin, MapId mapId, DrawingHandleWorld handle)
        {
            if (!_gunQ.TryComp(uid, out var gun) || gun.ShootCoordinates == null)
                return;

            var targetCoords = _transform.ToMapCoordinates(gun.ShootCoordinates.Value);
            if (targetCoords.MapId != mapId)
                return;

            handle.DrawLine(origin, targetCoords.Position, new Color(0.9f, 0.2f, 0.9f, 0.7f));
            handle.DrawCircle(targetCoords.Position, 0.08f, new Color(0.9f, 0.2f, 0.9f, 0.8f));
        }

        private Angle GetBaseRotation(EntityUid baseUid, Angle angleOffset)
        {
            var rotation = _transform.GetWorldRotation(baseUid);
            if (_moverQ.TryComp(baseUid, out var mover) && mover.CurrentDirection != Vector2i.Zero)
                rotation = new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

            return rotation + angleOffset;
        }

        private Angle GetVehicleFacingAngle(EntityUid vehicle, Angle vehicleRot)
        {
            if (_moverQ.TryComp(vehicle, out var mover) && mover.CurrentDirection != Vector2i.Zero)
                return new Vector2(mover.CurrentDirection.X, mover.CurrentDirection.Y).ToWorldAngle();

            return vehicleRot;
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

        private static Angle GetRenderFacing(
            VehicleTurretComponent turret,
            VehicleTurretComponent anchorTurret,
            Angle vehicleRot,
            Angle baseFacingAngle,
            Angle eyeRot)
        {
            return (GetOffsetFacing(turret, anchorTurret, vehicleRot, baseFacingAngle) + eyeRot).Reduced();
        }

        private static Angle GetOffsetFacing(
            VehicleTurretComponent turret,
            VehicleTurretComponent anchorTurret,
            Angle vehicleRot,
            Angle baseFacingAngle)
        {
            if (!turret.OffsetRotatesWithTurret)
                return baseFacingAngle;

            return (vehicleRot + anchorTurret.WorldRotation).Reduced();
        }

        private (Vector2 Offset, bool Rotate) GetOffset(
            GunMuzzleOffsetComponent muzzle,
            EntityUid baseUid,
            Angle baseRotation)
        {
            if (!muzzle.UseDirectionalOffsets)
                return (muzzle.Offset, true);

            var dir = GetBaseDirection(baseUid, baseRotation);
            var offset = dir switch
            {
                Direction.North => muzzle.OffsetNorth,
                Direction.East => muzzle.OffsetEast,
                Direction.South => muzzle.OffsetSouth,
                Direction.West => muzzle.OffsetWest,
                _ => muzzle.Offset,
            };

            return (offset, muzzle.RotateDirectionalOffsets);
        }

        private Direction GetBaseDirection(EntityUid baseUid, Angle baseRotation)
        {
            if (_moverQ.TryComp(baseUid, out var mover) && mover.CurrentDirection != Vector2i.Zero)
                return mover.CurrentDirection.AsDirection();

            return baseRotation.GetCardinalDir();
        }

        private bool TryGetVehicle(EntityUid turretUid, out EntityUid vehicle)
        {
            vehicle = default;
            var current = turretUid;

            while (_container.TryGetContainingContainer((current, null), out var container))
            {
                var owner = container.Owner;
                if (_ents.HasComponent<VehicleComponent>(owner))
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

            if (!_ents.HasComponent<VehicleTurretAttachmentComponent>(turretUid))
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
                if (_turretQ.TryComp(owner, out var turret))
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
}

