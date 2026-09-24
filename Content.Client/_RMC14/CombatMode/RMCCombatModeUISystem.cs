using Content.Client._RMC14.Emplacements;
using Content.Client.CombatMode;
using Content.Client.Hands.Systems;
using Content.Shared._RMC14.CombatMode;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.CombatMode;

public sealed class RMCCombatModeUISystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly CombatModeSystem _combatMode = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly RMCCombatModeSystem _rmcCombatMode = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly RMCWeaponControllerSystem _rmcSharedWeaponController = default!;

    private bool _crosshairsEnabled;
    private ICursor? _crosshairCursor;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, CCVars.CombatModeIndicatorsPointShow, v => _crosshairsEnabled = v, true);
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_ui.CurrentlyHovered is not IViewportControl)
            return;

        if (!_crosshairsEnabled || !_combatMode.IsInCombatMode())
        {
            _ui.CurrentlyHovered.CustomCursorShape = null;
            return;
        }

        var held = _hands.GetActiveHandEntity();
        if (_rmcSharedWeaponController.TryGetControllingWeapon(out var weapon))
            held = weapon;

        if (held == null || _rmcCombatMode.GetCrosshair(held.Value) == null)
        {
            _ui.CurrentlyHovered.CustomCursorShape = null;
            return;
        }

        _crosshairCursor ??= _clyde.CreateCursor(new Image<Rgba32>(1, 1), Vector2i.One);
        _ui.CurrentlyHovered.CustomCursorShape = _crosshairCursor;
    }
}
