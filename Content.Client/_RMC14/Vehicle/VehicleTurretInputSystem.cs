using System;
using System.Collections.Generic;
using Content.Client.CombatMode;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Vehicle;

public sealed class VehicleTurretInputSystem : EntitySystem
{
    private const float AimUpdateInterval = 0.1f;
    private static readonly Angle AimEpsilon = Angle.FromDegrees(1);

    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly VehicleTurretSystem _turrets = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, (Angle Angle, TimeSpan Time)> _lastAims = new();

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } user)
            return;

        if (!_combat.IsInCombatMode(user))
            return;

        if (!TryComp(user, out VehicleWeaponsOperatorComponent? operatorComp) ||
            operatorComp.Vehicle is not { } vehicle)
        {
            return;
        }

        if (!TryComp(vehicle, out RMCVehicleWeaponsComponent? weapons) ||
            weapons.Operator != user ||
            weapons.SelectedWeapon is not { } turretUid)
        {
            return;
        }

        if (!_turrets.TryResolveRotationTarget(turretUid, out var targetUid, out var targetTurret))
            return;

        if (!targetTurret.RotateToCursor)
            return;

        if (!_turrets.TryGetTurretOrigin(targetUid, targetTurret, out var originCoords))
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        var originMap = _transform.ToMapCoordinates(originCoords);
        var direction = mousePos.Position - originMap.Position;
        if (direction.LengthSquared() <= 0.0001f)
            return;

        var angle = direction.ToWorldAngle();

        if (_lastAims.TryGetValue(targetUid, out var last))
        {
            if ((_timing.CurTime - last.Time).TotalSeconds < AimUpdateInterval &&
                Math.Abs(Angle.ShortestDistance(angle, last.Angle).Degrees) < AimEpsilon.Degrees)
            {
                return;
            }
        }

        _lastAims[targetUid] = (angle, _timing.CurTime);

        var mouseCoords = _transform.ToCoordinates(mousePos);
        RaisePredictiveEvent(new VehicleTurretRotateEvent
        {
            Turret = GetNetEntity(turretUid),
            Coordinates = GetNetCoordinates(mouseCoords)
        });
    }
}
