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
    private const float AimUpdateInterval = 0.02f;
    private static readonly Angle AimEpsilon = Angle.FromDegrees(1);

    [Dependency] private readonly CombatModeSystem _combat = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VehicleTurretSystem _turrets = default!;

    private readonly Dictionary<EntityUid, (Angle Angle, TimeSpan Time)> _lastAims = new();
    private readonly Dictionary<EntityUid, MapCoordinates> _lastAimCoordinates = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleTurretComponent, EntityTerminatingEvent>(OnTurretTerminating);
    }

    private void OnTurretTerminating(Entity<VehicleTurretComponent> ent, ref EntityTerminatingEvent args)
    {
        _lastAims.Remove(ent);
        _lastAimCoordinates.Remove(ent);
    }

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } user)
            return;

        if (!_combat.IsInCombatMode(user))
            return;

        if (!TryComp(user, out VehicleWeaponsOperatorComponent? operatorComp) ||
            operatorComp.Vehicle is not { })
        {
            return;
        }

        if (operatorComp.SelectedWeapon is not { } turretUid)
        {
            return;
        }

        if (!TryComp(turretUid, out VehicleTurretComponent? sourceTurret) ||
            !_turrets.TryResolveRotationTarget(turretUid, out var targetUid, out var targetTurret))
            return;

        if (!targetTurret.RotateToCursor)
            return;

        if (!_turrets.TryGetTurretOrigin(targetUid, out var originCoords))
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        _lastAimCoordinates[turretUid] = mousePos;

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

    public bool TryGetLastAimCoordinates(EntityUid turretUid, out MapCoordinates coordinates)
    {
        return _lastAimCoordinates.TryGetValue(turretUid, out coordinates);
    }
}
