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

    public override void Initialize()
    {
        _overlay = new GridVehicleMoverOverlay(EntityManager);
        _overlay.DebugEnabled = _cfg.GetCVar(RMCCVars.RMCVehicleDebugOverlay);
        _overlay.CollisionsEnabled = _cfg.GetCVar(RMCCVars.RMCVehicleCollisionOverlay);

        _cfg.OnValueChanged(RMCCVars.RMCVehicleDebugOverlay, val =>
        {
            if (_overlay != null)
                _overlay.DebugEnabled = val;
        }, true);

        _cfg.OnValueChanged(RMCCVars.RMCVehicleCollisionOverlay, val =>
        {
            if (_overlay != null)
                _overlay.CollisionsEnabled = val;
        }, true);

        IoCManager.Resolve<IOverlayManager>().AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        if (_overlay != null)
            IoCManager.Resolve<IOverlayManager>().RemoveOverlay(_overlay);
    }
}
