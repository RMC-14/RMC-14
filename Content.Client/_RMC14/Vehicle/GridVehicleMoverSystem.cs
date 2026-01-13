using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.Vehicle;

public sealed class GridVehicleMoverSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public static readonly List<Vector2> DebugCollisionPositions = new();

    private GridVehicleMoverOverlay? _overlay;
    private VehicleHardpointDebugOverlay? _hardpointOverlay;

    public override void Initialize()
    {
        _overlay = new GridVehicleMoverOverlay(EntityManager);
        _overlay.DebugEnabled = _cfg.GetCVar(RMCCVars.RMCVehicleDebugOverlay);
        _overlay.CollisionsEnabled = _cfg.GetCVar(RMCCVars.RMCVehicleCollisionOverlay);
        _hardpointOverlay = new VehicleHardpointDebugOverlay(EntityManager)
        {
            Enabled = _cfg.GetCVar(RMCCVars.RMCVehicleHardpointOverlay)
        };

        _cfg.OnValueChanged(RMCCVars.RMCVehicleDebugOverlay, val =>
        {
            if (_overlay != null)
                _overlay.DebugEnabled = val;
        }, true);

        _cfg.OnValueChanged(RMCCVars.RMCVehicleHardpointOverlay, val =>
        {
            if (_hardpointOverlay != null)
                _hardpointOverlay.Enabled = val;
        }, true);

        _cfg.OnValueChanged(RMCCVars.RMCVehicleCollisionOverlay, val =>
        {
            if (_overlay != null)
                _overlay.CollisionsEnabled = val;
        }, true);

        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        overlayManager.AddOverlay(_overlay);
        overlayManager.AddOverlay(_hardpointOverlay);
    }

    public override void Shutdown()
    {
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (_overlay != null)
            overlayManager.RemoveOverlay(_overlay);
        if (_hardpointOverlay != null)
            overlayManager.RemoveOverlay(_hardpointOverlay);
    }
}
