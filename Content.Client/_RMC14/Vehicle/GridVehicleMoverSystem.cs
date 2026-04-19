using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Vehicle.Components;
using ClientPhysicsSystem = Robust.Client.Physics.PhysicsSystem;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Client.Physics;

namespace Content.Client.Vehicle;

public sealed class GridVehicleMoverSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ClientPhysicsSystem _physics = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public static readonly List<Vector2> DebugCollisionPositions = new();

    private GridVehicleMoverOverlay? _overlay;
    private VehicleHardpointDebugOverlay? _hardpointOverlay;
    private EntityUid? _lastPredictedVehicle;

    public override void Initialize()
    {
        _overlay = new GridVehicleMoverOverlay(EntityManager);
        _overlay.DebugEnabled = _cfg.GetCVar(RMCCVars.VehicleDebugOverlay);
        _overlay.CollisionsEnabled = _cfg.GetCVar(RMCCVars.VehicleCollisionOverlay);
        _overlay.MovementEnabled = _cfg.GetCVar(RMCCVars.VehicleMovementOverlay);
        RefreshSharedDebugFlags();
        _hardpointOverlay = new VehicleHardpointDebugOverlay(EntityManager)
        {
            Enabled = _cfg.GetCVar(RMCCVars.VehicleHardpointOverlay)
        };

        _cfg.OnValueChanged(RMCCVars.VehicleDebugOverlay, val =>
        {
            if (_overlay != null)
                _overlay.DebugEnabled = val;

            RefreshSharedDebugFlags();
            RefreshVehicleDebugOverlay();
        }, true);

        _cfg.OnValueChanged(RMCCVars.VehicleHardpointOverlay, val =>
        {
            if (_hardpointOverlay != null)
                _hardpointOverlay.Enabled = val;
        }, true);

        _cfg.OnValueChanged(RMCCVars.VehicleCollisionOverlay, val =>
        {
            if (_overlay != null)
                _overlay.CollisionsEnabled = val;

            RefreshSharedDebugFlags();
            RefreshVehicleDebugOverlay();
        }, true);

        _cfg.OnValueChanged(RMCCVars.VehicleMovementOverlay, val =>
        {
            if (_overlay != null)
                _overlay.MovementEnabled = val;

            RefreshSharedDebugFlags();
            RefreshVehicleDebugOverlay();
        }, true);

        SubscribeLocalEvent<GridVehicleMoverComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);

        RefreshVehicleDebugOverlay();
        _overlayManager.AddOverlay(_hardpointOverlay);
    }

    public override void Shutdown()
    {
        if (_overlay != null)
            _overlayManager.RemoveOverlay(_overlay);
        if (_hardpointOverlay != null)
            _overlayManager.RemoveOverlay(_hardpointOverlay);
    }

    private void RefreshSharedDebugFlags()
    {
        Content.Shared.Vehicle.GridVehicleMoverSystem.CollisionDebugEnabled =
            _overlay is { DebugEnabled: true } or { CollisionsEnabled: true };
        Content.Shared.Vehicle.GridVehicleMoverSystem.MovementDebugEnabled =
            _overlay is { MovementEnabled: true };
    }

    private void RefreshVehicleDebugOverlay()
    {
        if (_overlay == null)
            return;

        var enabled = _overlay.DebugEnabled || _overlay.CollisionsEnabled || _overlay.MovementEnabled;
        var hasOverlay = _overlayManager.HasOverlay<GridVehicleMoverOverlay>();
        if (enabled && !hasOverlay)
        {
            _overlayManager.AddOverlay(_overlay);
        }
        else if (!enabled && hasOverlay)
        {
            _overlayManager.RemoveOverlay(_overlay);
        }
    }

    private void OnUpdateIsPredicted(Entity<GridVehicleMoverComponent> ent, ref UpdateIsPredictedEvent args)
    {
        if (_playerManager.LocalEntity is not { } local)
            return;

        if (!TryComp(ent.Owner, out VehicleComponent? vehicle))
            return;

        if (vehicle.Operator == local)
            args.IsPredicted = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_playerManager.LocalEntity is not { } local)
        {
            if (_lastPredictedVehicle is { } oldVehicle)
                _physics.UpdateIsPredicted(oldVehicle);
            _lastPredictedVehicle = null;
            return;
        }

        if (TryComp(local, out VehicleOperatorComponent? op) && op.Vehicle is { } vehicle)
        {
            if (_lastPredictedVehicle != vehicle)
            {
                if (_lastPredictedVehicle is { } oldVehicle)
                    _physics.UpdateIsPredicted(oldVehicle);

                _lastPredictedVehicle = vehicle;
                _physics.UpdateIsPredicted(vehicle);
            }
            return;
        }

        if (_lastPredictedVehicle is { } oldPredicted)
        {
            _physics.UpdateIsPredicted(oldPredicted);
            _lastPredictedVehicle = null;
        }
    }
}
