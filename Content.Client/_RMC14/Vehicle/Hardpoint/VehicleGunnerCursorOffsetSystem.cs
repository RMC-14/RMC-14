using System.Collections.Generic;
using System.Numerics;
using Content.Client.Movement.Systems;
using Content.Shared.Camera;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Movement.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Vehicle.Hardpoint;

public sealed class VehicleGunnerCursorOffsetSystem : EntitySystem
{
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly Dictionary<EntityUid, Vector2> _currentPositions = new();
    private static readonly float EdgeOffset = 0.9f;

    public override void Initialize()
    {
        UpdatesBefore.Add(typeof(Content.Client._RMC14.Vehicle.VehicleTurretInputSystem));
        UpdatesBefore.Add(typeof(Content.Client.Weapons.Ranged.Systems.GunSystem));
        SubscribeLocalEvent<VehicleGunnerViewUserComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
    }

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity is not { } user)
            return;

        if (!TryComp<VehicleGunnerViewUserComponent>(user, out var gunner) ||
            gunner.CursorMaxOffset <= 0f ||
            !TryComp<EyeComponent>(user, out var eye))
        {
            return;
        }

        _contentEye.UpdateEyeOffset((user, eye));
    }

    private void OnGetEyeOffset(Entity<VehicleGunnerViewUserComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (ent.Comp.CursorMaxOffset <= 0f)
        {
            _currentPositions.Remove(ent.Owner);
            return;
        }

        var offset = OffsetAfterMouse(ent.Owner, ent.Comp);
        if (offset == null)
            return;

        args.Offset += offset.Value;
    }

    private Vector2? OffsetAfterMouse(EntityUid uid, VehicleGunnerViewUserComponent component)
    {
        var mousePos = _inputManager.MouseScreenPosition;
        if (mousePos.Window == WindowId.Invalid)
            return _currentPositions.GetValueOrDefault(uid, Vector2.Zero);

        var screenSize = _clyde.MainWindow.Size;
        var minValue = MathF.Min(screenSize.X / 2f, screenSize.Y / 2f) * EdgeOffset;
        if (minValue <= 0f)
            return _currentPositions.GetValueOrDefault(uid, Vector2.Zero);

        var mouseNormalizedPos = new Vector2(
            -(mousePos.X - screenSize.X / 2f) / minValue,
            (mousePos.Y - screenSize.Y / 2f) / minValue);

        var eyeRotation = _eyeManager.CurrentEye.Rotation;
        var mouseActualRelativePos = Vector2.Transform(
            mouseNormalizedPos,
            System.Numerics.Quaternion.CreateFromAxisAngle(-System.Numerics.Vector3.UnitZ, (float) eyeRotation.Opposite().Theta));

        mouseActualRelativePos *= component.CursorMaxOffset;
        if (mouseActualRelativePos.Length() > component.CursorMaxOffset)
            mouseActualRelativePos = mouseActualRelativePos.Normalized() * component.CursorMaxOffset;

        var current = _currentPositions.GetValueOrDefault(uid, Vector2.Zero);
        if (current != mouseActualRelativePos)
        {
            var vectorOffset = mouseActualRelativePos - current;
            if (vectorOffset.Length() > component.CursorOffsetSpeed)
                vectorOffset = vectorOffset.Normalized() * component.CursorOffsetSpeed;

            current += vectorOffset;
            _currentPositions[uid] = current;
        }

        return current;
    }
}
